using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Moq;
using Stratis.SmartContracts;
using Stratis.SmartContracts.CLR;

namespace OpdexProposalVoteTests
{
    public class BaseContractTest
    {
        public readonly Mock<ISmartContractState> _mockContractState;
        private readonly Mock<IContractLogger> _mockContractLogger;
        private readonly Mock<IInternalTransactionExecutor> _mockInternalExecutor;
        private readonly Mock<ISerializer> _mockSerializer;
        protected readonly Address _contract;
        protected readonly Address _sender;
        protected readonly Address _mn1;
        protected readonly Address _mn2;
        protected readonly Address _mn3;
        protected readonly InMemoryState _persistentState;

        protected BaseContractTest()
        {
            _persistentState = new InMemoryState();
            _mockContractLogger = new Mock<IContractLogger>();
            _mockContractState = new Mock<ISmartContractState>();
            _mockInternalExecutor = new Mock<IInternalTransactionExecutor>();
            _mockSerializer = new Mock<ISerializer>();
            _mockContractState.Setup(x => x.PersistentState).Returns(_persistentState);
            _mockContractState.Setup(x => x.ContractLogger).Returns(_mockContractLogger.Object);
            _mockContractState.Setup(x => x.InternalTransactionExecutor).Returns(_mockInternalExecutor.Object);
            _mockContractState.Setup(x => x.Serializer).Returns(_mockSerializer.Object);
            _contract = "0x0000000000000000000000000000000000000001".HexToAddress();
            _sender = "0x0000000000000000000000000000000000000002".HexToAddress();
            _mn1 = "0x0000000000000000000000000000000000000003".HexToAddress();
            _mn2 = "0x0000000000000000000000000000000000000004".HexToAddress();
            _mn3 = "0x0000000000000000000000000000000000000005".HexToAddress();
        }

        protected PrivateYesNoVote CreateNewVoteContract(ulong currentBlock = 99999, ulong endBlock = 100000)
        {
            _mockContractState.Setup(x => x.Message).Returns(new Message(_contract, _sender, 0));
            _mockContractState.Setup(x => x.Block.Number).Returns(currentBlock);

            var masterNodesList = new[] {_mn1, _mn2, _mn3};
            var bytesList = ObjectToByteArray(masterNodesList.ToString());
            
            _mockContractState.Setup(s => s.Serializer.ToArray<Address>(bytesList)).Returns(() => masterNodesList);

            return new PrivateYesNoVote(_mockContractState.Object, endBlock, bytesList);
        }

        protected void SetupMessage(Address contractAddress, Address sender, ulong value = 0)
        {
            _mockContractState.Setup(x => x.Message).Returns(new Message(contractAddress, sender, value));
        }

        protected void SetupBalance(ulong balance)
        {
            _mockContractState.Setup(x => x.GetBalance).Returns(() => balance);
        }

        protected void SetupCall(Address to, ulong amountToTransfer, string methodName, object[] parameters, TransferResult result)
        {
            _mockInternalExecutor
                .Setup(x => x.Call(_mockContractState.Object, to, amountToTransfer, methodName, parameters, It.IsAny<ulong>()))
                .Returns(result);
        }

        protected void SetupTransfer(Address to, ulong value, TransferResult result)
        {
            _mockInternalExecutor
                .Setup(x => x.Transfer(_mockContractState.Object, to, value))
                .Returns(result);
        }

        protected void SetupCreate<T>(CreateResult result, ulong amount = 0ul, object[] parameters = null)
        {
            _mockInternalExecutor
                .Setup(x => x.Create<T>(_mockContractState.Object, amount, parameters, It.IsAny<ulong>()))
                .Returns(result);
        }

        protected void VerifyCall(Address addressTo, ulong amountToTransfer, string methodName, object[] parameters, Func<Times> times)
        {
            _mockInternalExecutor.Verify(x => x.Call(_mockContractState.Object, addressTo, amountToTransfer, methodName, parameters, 0ul), times);
        }

        protected void VerifyTransfer(Address to, ulong value, Func<Times> times)
        {
            _mockInternalExecutor.Verify(x => x.Transfer(_mockContractState.Object, to, value), times);
        }

        protected void VerifyLog<T>(T expectedLog, Func<Times> times) where T : struct
        {
            _mockContractLogger.Verify(x => x.Log(_mockContractState.Object, expectedLog), times);
        }
        
        protected static byte[] ObjectToByteArray(object obj)
        {
            if(obj == null) return null;
            BinaryFormatter bf = new BinaryFormatter();
            using MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }
}