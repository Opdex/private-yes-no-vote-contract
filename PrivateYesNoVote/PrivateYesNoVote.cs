using Stratis.SmartContracts;

public class PrivateYesNoVote : SmartContract
{
    public PrivateYesNoVote(ISmartContractState smartContractState, ulong votePeriodEndBlock, byte[] addressesBytes) 
        : base(smartContractState)
    {
        Assert(votePeriodEndBlock > Block.Number, "Voting period end block must be greater than current block.");
        
        VotePeriodEndBlock = votePeriodEndBlock;
        Owner = Message.Sender;

        WhiteListAddressesExecute(addressesBytes);
    }

    public Address Owner
    {
        get => PersistentState.GetAddress(nameof(Owner));
        private set => PersistentState.SetAddress(nameof(Owner), value); 
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
        PersistentState.SetBool($"Authorized:{address}", true);
    }

    public bool IsAuthorized(Address address)
    {
        return PersistentState.GetBool($"Authorized:{address}");
    }

    public string GetVote(Address address)
    {
        return PersistentState.GetString($"Vote:{address}");
    }

    public void WhitelistAddresses(byte[] addressBytes)
    {
        Assert(Message.Sender == Owner, "Must be contract owner to whitelist addresses");
        WhiteListAddressesExecute(addressBytes);
    }

    private void WhiteListAddressesExecute(byte[] addressBytes)
    {
        var addresses = Serializer.ToArray<Address>(addressBytes);
        
        foreach (var address in addresses)
        {
            SetAuthorization(address);
        }
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
        
        Log(new VoteEvent { Voter = Message.Sender, Vote = vote });
    }

    public struct VoteEvent
    {
        [Index]
        public Address Voter;
        public string Vote;
    }
}