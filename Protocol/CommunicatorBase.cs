

namespace EssSimulator
{
    public class CommunicatorBase
    {
        protected DeviceInfoDto deviceInfoDto;
        public CommunicatorBase(DeviceInfoDto deviceInfoDto)
        {
            this.deviceInfoDto = deviceInfoDto;
        }

        public virtual int GetCommunicatorStatus()
        {
            return 0;
        }

        public virtual void Connect()
        {
            
        }

        public virtual void Disconnect()
        {
            
        }
    }
}