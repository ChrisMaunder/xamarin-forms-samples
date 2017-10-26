using System;

namespace Robotics.Mobile.Core.Bluetooth.LE
{
	[Flags]
	public enum CharacteristicPropertyType
	{
		// Superset                          Hex     Apple                       Android

		Broadcast                  = 1,   // 0x001   Broadcast                   Broadcast
        Read                       = 2,   // 0x002   Read                        Read
        AppleWriteWithoutResponse  = 4,   // 0x004   WriteWithoutResponse
        WriteWithoutResponse       = 8,   // 0x008   PropertyWrite               Write
        Notify                     = 16,  // 0x010   Notify                      Notify
        Indicate                   = 32,  // 0x020   Indicate                    Indicate
        AuthenticatedSignedWrites  = 64,  // 0x040   AuthenticatedSignedWrites   SignedWrite
        ExtendedProperties         = 128, // 0x080   ExtendedProperties          ExtendedProperties
        NotifyEncryptionRequired   = 256, // 0x100   NotifyEncryptionRequired
        IndicateEncryptionRequired = 512, // 0x200   IndicateEncryptionRequired
	}
}

