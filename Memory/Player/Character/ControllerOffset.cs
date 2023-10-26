namespace TroveSkip.Memory.Player.Character
{
    public enum ControllerOffset
    { 
        PositionX = 0x80,
        PositionY = 0x84,
        PositionZ = 0x88,
        VelocityX = 0xB0,
        VelocityY = 0xB4,
        VelocityZ = 0xB8,
        Gravity = 0xD8
        //+20h
        //1c4 smth with turn
        //1D0 blocks turning
    }
}