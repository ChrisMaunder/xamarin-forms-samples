using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;

namespace Robotics.Messaging
{
    public class ControlClient
    {
        readonly ObservableCollection<Command>  commands  = new ObservableCollection<Command>();
        readonly ObservableCollection<Variable> variables = new ObservableCollection<Variable>();
        readonly Stream        stream;
        readonly TaskScheduler scheduler;

        private int eid = 1;

        class ClientVariable : Variable, INotifyPropertyChanged
        {
            public ControlClient Client;
            public event PropertyChangedEventHandler PropertyChanged = delegate { };

            public override object Value
            {
                get
                {
                    return base.Value;
                }

                set
                {
                    if (!IsWriteable)
                        return;

                    var oldValue = base.Value;
                    if (oldValue != null && oldValue.Equals(value))
                        return;

                    Client.SetVariableValueAsync(this, value);

                    base.Value = value;
                }
            }

            public override void SetValue(object newVal)
            {
                var oldValue = base.Value;
                if (oldValue != null && oldValue.Equals(newVal))
                    return;

                base.SetValue(newVal);

                Client.Schedule(() => PropertyChanged(this, new PropertyChangedEventArgs("Value")));
            }
        }

        void Schedule(Action action)
        {
            Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, scheduler);
        }

        public IList<Variable> Variables 
        { 
            get { return variables; } 
        }

        public IList<Command> Commands 
        { 
            get { return commands; } 
        }

        public ControlClient(Stream stream)
        {
            this.stream = stream;
            scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        Task GetVariablesAsync()
        {
            Debug.WriteLine("ControlClient.GetVariablesAsync");
            return (new Message((byte)ControlOp.GetVariables)).WriteAsync(stream);
        }

        Task GetCommandsAsync()
        {
            Debug.WriteLine("ControlClient.GetCommandsAsync");
            return (new Message((byte)ControlOp.GetCommands)).WriteAsync(stream);
        }

        Task SetVariableValueAsync(ClientVariable variable, object value)
        {
            // This is not async because it's always reading from a cache
            // Variable updates come asynchronously
            return (new Message((byte)ControlOp.SetVariableValue, variable.Id, value)).WriteAsync(stream);
        }

        public Task ExecuteCommandAsync(Command command)
        {
            return (new Message((byte)ControlOp.ExecuteCommand, command.Id, eid++)).WriteAsync(stream);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await GetVariablesAsync();
            await GetCommandsAsync();

            var message = new Message();

            while (!cancellationToken.IsCancellationRequested)
            {
                await message.ReadAsync(stream);

                Debug.WriteLine("Got message: " + (ControlOp)message.Operation + "(" + 
                                string.Join(", ", message.Arguments.Select(x => x.ToString())) + ")");

                switch ((ControlOp)message.Operation)
                {
                    case ControlOp.Variable:
                        {
                            var id       = (int)message.Arguments[0];
                            var variable = variables.FirstOrDefault(x => x.Id == id);

                            if (variable == null)
                            {
                                var clientVariable = new ClientVariable
                                {
                                    Client      = this,
                                    Id          = id,
                                    Name        = (string)message.Arguments[1],
                                    IsWriteable = (bool)message.Arguments[2],
                                };

                                clientVariable.SetValue(message.Arguments[3]);
                                variable = clientVariable;

                                Schedule(() => variables.Add(variable));
                            }
                        }
                        break;

                    case ControlOp.VariableValue:
                        {
                            var id = (int)message.Arguments[0];

                            if (variables.FirstOrDefault(x => x.Id == id) is ClientVariable clientVariable)
                            {
                                var newVal = message.Arguments[1];
                                Schedule(() => clientVariable.SetValue(newVal));
                            }
                            else
                            {
                                await GetVariablesAsync();
                            }
                        }
                        break;

                    case ControlOp.Command:
                        {
                            var id = (int)message.Arguments[0];
                            var command = commands.FirstOrDefault(x => x.Id == id);
                            if (command == null)
                            {
                                var clientCommand = new Command
                                {
                                    Id   = id,
                                    Name = (string)message.Arguments[1],
                                };

                                command = clientCommand;
                                Schedule(() => commands.Add(command));
                            }
                        }
                        break;

                    default:
                        Debug.WriteLine ("Ignoring message: " + message.Operation);
                        break;
                }
            }
        }
    }
}
