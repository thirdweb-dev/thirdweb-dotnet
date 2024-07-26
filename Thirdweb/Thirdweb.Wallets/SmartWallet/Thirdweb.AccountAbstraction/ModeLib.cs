namespace Thirdweb.AccountAbstraction
{
    public static class ModeLib
    {
        public struct ModeCode
        {
            public byte[] Value;

            public ModeCode(byte[] value)
            {
                if (value.Length != 32)
                    throw new ArgumentException("ModeCode must be 32 bytes.");
                Value = value;
            }
        }

        public struct CallType
        {
            public byte Value;

            public CallType(byte value)
            {
                Value = value;
            }
        }

        public struct ExecType
        {
            public byte Value;

            public ExecType(byte value)
            {
                Value = value;
            }
        }

        public struct ModeSelector
        {
            public byte[] Value;

            public ModeSelector(byte[] value)
            {
                if (value.Length != 4)
                    throw new ArgumentException("ModeSelector must be 4 bytes.");
                Value = value;
            }
        }

        public struct ModePayload
        {
            public byte[] Value;

            public ModePayload(byte[] value)
            {
                if (value.Length != 22)
                    throw new ArgumentException("ModePayload must be 22 bytes.");
                Value = value;
            }
        }

        // Constants
        public static readonly CallType CALLTYPE_SINGLE = new CallType(0x00);
        public static readonly ExecType EXECTYPE_DEFAULT = new ExecType(0x00);
        public static readonly ExecType EXECTYPE_TRY = new ExecType(0x01);
        public static readonly ModeSelector MODE_DEFAULT = new ModeSelector(new byte[] { 0x00, 0x00, 0x00, 0x00 });
        public static readonly ModePayload MODE_PAYLOAD_DEFAULT = new ModePayload(new byte[22]);

        // Encode function
        public static ModeCode Encode(CallType callType, ExecType execType, ModeSelector modeSelector, ModePayload payload)
        {
            var result = new byte[32];
            result[0] = callType.Value;
            result[1] = execType.Value;

            Array.Copy(modeSelector.Value, 0, result, 6, 4); // ModeSelector is placed starting from byte 6
            Array.Copy(payload.Value, 0, result, 10, 22); // ModePayload is placed starting from byte 10

            return new ModeCode(result);
        }

        // EncodeSimpleSingle function
        public static ModeCode EncodeSimpleSingle()
        {
            return Encode(CALLTYPE_SINGLE, EXECTYPE_DEFAULT, MODE_DEFAULT, MODE_PAYLOAD_DEFAULT);
        }

        public static ModeCode EncodeSimpleSingleTry()
        {
            return Encode(CALLTYPE_SINGLE, EXECTYPE_TRY, MODE_DEFAULT, MODE_PAYLOAD_DEFAULT);
        }
    }
}
