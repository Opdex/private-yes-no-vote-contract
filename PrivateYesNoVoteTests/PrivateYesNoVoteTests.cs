using FluentAssertions;
using Moq;
using OpdexProposalVoteTests;
using Stratis.SmartContracts;
using Xunit;

namespace PrivateYesNoVoteTests
{
    public class PrivateYesNoVoteTests : BaseContractTest
    {
        [Fact]
        public void CreatesVoteContract_Success()
        {
            var voteContract = CreateNewVoteContract();

            voteContract.VotePeriodEndBlock.Should().Be(100000);
            voteContract.IsAuthorized(AddressOne).Should().BeTrue();
            voteContract.IsAuthorized(AddressTwo).Should().BeTrue();
            voteContract.IsAuthorized(AddressThree).Should().BeTrue();
        }

        [Fact]
        public void CanVoteYes_Success()
        {
            var sender = AddressOne;
            const string vote = "yes";
            var voteContract = CreateNewVoteContract();
            
            SetupMessage(Contract, sender);

            voteContract.YesVotes.Should().Be(0);
            voteContract.NoVotes.Should().Be(0);
            
            voteContract.Vote(vote);

            voteContract.NoVotes.Should().Be(0);
            voteContract.YesVotes.Should().Be(1);

            VerifyLog(new PrivateYesNoVote.VoteEvent {MasterNode = AddressOne, Vote = vote}, Times.Once);
        }
        
        [Fact]
        public void CanVoteNo_Success()
        {
            var voteContract = CreateNewVoteContract();
            
            SetupMessage(Contract, AddressOne);

            voteContract.YesVotes.Should().Be(0);
            voteContract.NoVotes.Should().Be(0);
            
            voteContract.Vote("no");

            voteContract.NoVotes.Should().Be(1);
            voteContract.YesVotes.Should().Be(0);
        }

        [Fact]
        public void CanVote_UpdatesCounts_Success()
        {
            var voteContract = CreateNewVoteContract();
            
            voteContract.YesVotes.Should().Be(0);
            voteContract.NoVotes.Should().Be(0);
            
            // MN 1
            SetupMessage(Contract, AddressOne);
            voteContract.Vote("no");
            
            // MN 2
            SetupMessage(Contract, AddressTwo);
            voteContract.Vote("yes");
            
            // MN 3
            SetupMessage(Contract, AddressThree);
            voteContract.Vote("yes");

            voteContract.YesVotes.Should().Be(2);
            voteContract.NoVotes.Should().Be(1);
        }

        [Fact]
        public void CanVote_Throws_AlreadyVoted()
        {
            var voteContract = CreateNewVoteContract();
            
            SetupMessage(Contract, AddressOne);
            voteContract.Vote("no");
            
            voteContract.NoVotes.Should().Be(1);
            
            voteContract
                .Invoking(v => v.Vote("yes"))
                .Should().Throw<SmartContractAssertException>()
                .WithMessage("Sender has already voted.");
        }

        [Fact]
        public void CanVote_Throws_NotAuthorized()
        {
            var voteContract = CreateNewVoteContract();
            
            SetupMessage(Contract, Sender);
            
            voteContract
                .Invoking(v => v.Vote("Yes"))
                .Should().Throw<SmartContractAssertException>()
                .WithMessage("Sender is not authorized to vote.");
        }

        [Fact]
        public void CanVote_Throws_InvalidVote()
        {
            var voteContract = CreateNewVoteContract();
            
            SetupMessage(Contract, AddressOne);
            
            voteContract
                .Invoking(v => v.Vote("Maybe"))
                .Should().Throw<SmartContractAssertException>()
                .WithMessage("Invalid vote option");
        }

        [Fact]
        public void CanVote_Throws_VotPeriodEnded()
        {
            var voteContract = CreateNewVoteContract();

            SetupMessage(Contract, AddressOne);
            SetupBlock(100001);

            voteContract
                .Invoking(v => v.Vote("yes"))
                .Should().Throw<SmartContractAssertException>()
                .WithMessage("Voting period has ended.");
        }

        [Fact]
        public void CreateContract_Throws_InvalidVoteEndBlock()
        {
            try
            {
                CreateNewVoteContract(2, 2);
                
                // Intentionally fail the test if we reach here
                false.Should().BeTrue();
            }
            catch (SmartContractAssertException ex)
            {
                ex.Message.Should().Be("Voting period end block must be greater than current block.");
            }
        }
    }
}