using System;
using System.Linq;
using System.Collections.Generic;

namespace Thirdweb.AccountAbstraction
{
    public static class ExecutionLib
    {
        public struct Execution
        {
            public byte[] Target { get; set; } // Address is 20 bytes
            public byte[] Value { get; set; } // 32 bytes for uint256
            public byte[] CallData { get; set; }

            public Execution(byte[] target, byte[] value, byte[] callData)
            {
                if (target.Length != 20)
                    throw new ArgumentException("Target must be 20 bytes.");
                if (value.Length != 32)
                    throw new ArgumentException("Value must be 32 bytes.");
                Target = target;
                Value = value;
                CallData = callData;
            }
        }

        public static Execution[] DecodeBatch(byte[] callData)
        {
            int offset = BitConverter.ToInt32(callData.Take(4).Reverse().ToArray(), 0);
            int length = BitConverter.ToInt32(callData.Skip(offset).Take(4).Reverse().ToArray(), 0);
            Execution[] executionBatch = new Execution[length];

            int currentIndex = offset + 4;
            for (int i = 0; i < length; i++)
            {
                byte[] target = callData.Skip(currentIndex).Take(20).ToArray();
                byte[] value = callData.Skip(currentIndex + 20).Take(32).ToArray();
                byte[] executionCallData = callData.Skip(currentIndex + 52).ToArray();

                executionBatch[i] = new Execution(target, value, executionCallData);
                currentIndex += 52 + executionCallData.Length;
            }

            return executionBatch;
        }

        public static byte[] EncodeBatch(Execution[] executions)
        {
            var batchData = new List<byte>();

            foreach (var execution in executions)
            {
                batchData.AddRange(execution.Target);
                batchData.AddRange(execution.Value);
                batchData.AddRange(execution.CallData);
            }

            byte[] callDataLength = BitConverter.GetBytes(batchData.Count).Reverse().ToArray();
            return callDataLength.Concat(batchData).ToArray();
        }

        public static (byte[] target, byte[] value, byte[] callData) DecodeSingle(byte[] executionCalldata)
        {
            byte[] target = executionCalldata.Take(20).ToArray();
            byte[] value = executionCalldata.Skip(20).Take(32).ToArray();
            byte[] callData = executionCalldata.Skip(52).ToArray();

            return (target, value, callData);
        }

        public static byte[] EncodeSingle(byte[] target, byte[] value, byte[] callData)
        {
            if (target.Length != 20)
                throw new ArgumentException("Target must be 20 bytes.");
            if (value.Length != 32)
                throw new ArgumentException("Value must be 32 bytes.");

            return target.Concat(value).Concat(callData).ToArray();
        }
    }
}
