using Foundation;
using UIKit;

using Robotics.Mobile.Core.Bluetooth.LE;
using Xamarin.Forms.Platform.iOS;

namespace HeartRateMonitor.iOS
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : FormsApplicationDelegate
	{
		//UIWindow window;

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			Xamarin.Forms.Forms.Init ();

			//window = new UIWindow (UIScreen.MainScreen.Bounds);

			App.SetAdapter (Adapter.Current);

			LoadApplication (new App ());

			UINavigationBar.Appearance.TintColor = UIColor.Red;

			return base.FinishedLaunching(app, options);
		}
	}
}

