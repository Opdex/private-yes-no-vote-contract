using Stratis.SmartContracts;

public class PrivateYesNoVote : SmartContract
{
    public PrivateYesNoVote(ISmartContractState smartContractState, ulong votePeriodEndBlock, byte[] masterNodes) 
        : base(smartContractState)
    {
        VotePeriodEndBlock = votePeriodEndBlock;
        var masterNodesList = Serializer.ToArray<Address>(masterNodes);

        foreach (var masterNode in masterNodesList)
        {
            SetAuthorization(masterNode);
        }
    }

    public ulong VotePeriodEndBlock
    {
        get => PersistentState.GetUInt64(nameof(VotePeriodEndBlock));
        private set => PersistentState.SetUInt64(nameof(VotePeriodEndBlock), value);
    }

    public uint YesVotes
    {
        get => PersistentState.GetUInt32(nameof(YesVotes));
        private set => PersistentState.SetUInt32(nameof(YesVotes), value);
    }
    
    public uint NoVotes
    {
        get => PersistentState.GetUInt32(nameof(NoVotes));
        private set => PersistentState.SetUInt32(nameof(NoVotes), value);
    }

    private void SetAuthorization(Address address)
    {
        PersistentState.SetBool($"MasterNode:{address}", true);
    }

    public bool IsAuthorized(Address address)
    {
        return PersistentState.GetBool($"MasterNode:{address}");
    }

    public string GetVote(Address address)
    {
        return PersistentState.GetString($"Vote:{address}");
    }

    private void SetVote(Address address, string vote)
    {
        PersistentState.SetString($"Vote:{address}", vote);

        if (vote == "yes") YesVotes++;
        else NoVotes++;
    }

    public void Vote(string vote)
    {
        vote = vote.ToLower();
        
        Assert(IsAuthorized(Message.Sender), "Sender is not authorized to vote.");
        Assert(string.IsNullOrWhiteSpace(GetVote(Message.Sender)), "Sender has already voted.");
        Assert(Block.Number <= VotePeriodEndBlock, "Voting period has ended.");
        Assert(vote == "yes" || vote == "no", "Invalid vote option");
        
        SetVote(Message.Sender, vote);
        
        Log(new VoteEvent { MasterNode = Message.Sender, Vote = vote });
    }

    public struct VoteEvent
    {
        public Address MasterNode;
        public string Vote;
    }
}