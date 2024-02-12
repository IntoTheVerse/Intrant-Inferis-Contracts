using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Solana.Unity;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using IntrantInferis;
using IntrantInferis.Program;
using IntrantInferis.Errors;
using IntrantInferis.Accounts;

namespace IntrantInferis
{
    namespace Accounts
    {
        public partial class Player
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 15766710478567431885UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{205, 222, 112, 7, 165, 155, 206, 218};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "bSBoKNsSHuj";
            public string Username { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey CurrentPlayerCharacter { get; set; }

            public ulong LastTransactionTime { get; set; }

            public byte LastRaffleValue { get; set; }

            public ulong LastRaffleClaimTime { get; set; }

            public byte LastCheckpointCleared { get; set; }

            public static Player Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Player result = new Player();
                offset += _data.GetBorshString(offset, out var resultUsername);
                result.Username = resultUsername;
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                result.CurrentPlayerCharacter = _data.GetPubKey(offset);
                offset += 32;
                result.LastTransactionTime = _data.GetU64(offset);
                offset += 8;
                result.LastRaffleValue = _data.GetU8(offset);
                offset += 1;
                result.LastRaffleClaimTime = _data.GetU64(offset);
                offset += 8;
                result.LastCheckpointCleared = _data.GetU8(offset);
                offset += 1;
                return result;
            }
        }

        public partial class PlayerCharacter
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 7469301213757221832UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{200, 171, 100, 62, 225, 73, 168, 103};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "aZkEsbecvkn";
            public PublicKey Owner { get; set; }

            public PublicKey NftAddress { get; set; }

            public bool Locked { get; set; }

            public ulong MaxHp { get; set; }

            public ulong MaxStamina { get; set; }

            public ulong LastLockedTime { get; set; }

            public static PlayerCharacter Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                PlayerCharacter result = new PlayerCharacter();
                result.Owner = _data.GetPubKey(offset);
                offset += 32;
                result.NftAddress = _data.GetPubKey(offset);
                offset += 32;
                result.Locked = _data.GetBool(offset);
                offset += 1;
                result.MaxHp = _data.GetU64(offset);
                offset += 8;
                result.MaxStamina = _data.GetU64(offset);
                offset += 8;
                result.LastLockedTime = _data.GetU64(offset);
                offset += 8;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum IntrantInferisErrorKind : uint
        {
            WrongAuthority = 6000U,
            WrongCheckpoint = 6001U
        }
    }

    public partial class IntrantInferisClient : TransactionalBaseClient<IntrantInferisErrorKind>
    {
        public IntrantInferisClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId) : base(rpcClient, streamingRpcClient, programId)
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Player>>> GetPlayersAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Player.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Player>>(res);
            List<Player> resultingAccounts = new List<Player>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Player.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Player>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerCharacter>>> GetPlayerCharactersAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = PlayerCharacter.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerCharacter>>(res);
            List<PlayerCharacter> resultingAccounts = new List<PlayerCharacter>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => PlayerCharacter.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerCharacter>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Player>> GetPlayerAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Player>(res);
            var resultingAccount = Player.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Player>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<PlayerCharacter>> GetPlayerCharacterAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerCharacter>(res);
            var resultingAccount = PlayerCharacter.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerCharacter>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribePlayerAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Player> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Player parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Player.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribePlayerCharacterAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, PlayerCharacter> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                PlayerCharacter parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = PlayerCharacter.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<RequestResult<string>> SendInitializePlayerAsync(InitializePlayerAccounts accounts, string username, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.IntrantInferisProgram.InitializePlayer(accounts, username, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendInitializePlayerCharacterAsync(InitializePlayerCharacterAccounts accounts, PublicKey nftAddress, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.IntrantInferisProgram.InitializePlayerCharacter(accounts, nftAddress, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendLockPlayerCharacterAsync(LockPlayerCharacterAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.IntrantInferisProgram.LockPlayerCharacter(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendSetCurrentPlayerCharacterAsync(SetCurrentPlayerCharacterAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.IntrantInferisProgram.SetCurrentPlayerCharacter(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendUpdatePlayerCheckpointAsync(UpdatePlayerCheckpointAccounts accounts, byte checkpoint, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.IntrantInferisProgram.UpdatePlayerCheckpoint(accounts, checkpoint, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendClaimRaffleAsync(ClaimRaffleAccounts accounts, byte raffleType, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.IntrantInferisProgram.ClaimRaffle(accounts, raffleType, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendAddTokenAsync(AddTokenAccounts accounts, ulong amount, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.IntrantInferisProgram.AddToken(accounts, amount, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendReduceTokenAsync(ReduceTokenAccounts accounts, ulong amount, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.IntrantInferisProgram.ReduceToken(accounts, amount, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        protected override Dictionary<uint, ProgramError<IntrantInferisErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<IntrantInferisErrorKind>>{{6000U, new ProgramError<IntrantInferisErrorKind>(IntrantInferisErrorKind.WrongAuthority, "Wrong Authority")}, {6001U, new ProgramError<IntrantInferisErrorKind>(IntrantInferisErrorKind.WrongCheckpoint, "Updated Checkpoint can't be smaller")}, };
        }
    }

    namespace Program
    {
        public class InitializePlayerAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class InitializePlayerCharacterAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey PlayerCharacterAccount { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class LockPlayerCharacterAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey PlayerCharacterAccount { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class SetCurrentPlayerCharacterAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey PlayerCharacterAccount { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class UpdatePlayerCheckpointAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class ClaimRaffleAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey SignerWallet { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey VaultPda { get; set; }

            public PublicKey VaultAta { get; set; }

            public PublicKey PlayerAta { get; set; }

            public PublicKey GameToken { get; set; }

            public PublicKey TokenProgram { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class AddTokenAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey VaultPda { get; set; }

            public PublicKey VaultAta { get; set; }

            public PublicKey PlayerAta { get; set; }

            public PublicKey GameToken { get; set; }

            public PublicKey TokenProgram { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class ReduceTokenAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey SignerWallet { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey VaultPda { get; set; }

            public PublicKey VaultAta { get; set; }

            public PublicKey PlayerAta { get; set; }

            public PublicKey GameToken { get; set; }

            public PublicKey TokenProgram { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public static class IntrantInferisProgram
        {
            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializePlayer(InitializePlayerAccounts accounts, string username, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(9239203753139697999UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(username, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializePlayerCharacter(InitializePlayerCharacterAccounts accounts, PublicKey nftAddress, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerCharacterAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(2749334999198737660UL, offset);
                offset += 8;
                _data.WritePubKey(nftAddress, offset);
                offset += 32;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction LockPlayerCharacter(LockPlayerCharacterAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerCharacterAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(967749446419166609UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction SetCurrentPlayerCharacter(SetCurrentPlayerCharacterAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerCharacterAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(16645996380804696787UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction UpdatePlayerCheckpoint(UpdatePlayerCheckpointAccounts accounts, byte checkpoint, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(2282445561284346574UL, offset);
                offset += 8;
                _data.WriteU8(checkpoint, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ClaimRaffle(ClaimRaffleAccounts accounts, byte raffleType, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.SignerWallet, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.VaultPda, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.VaultAta, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerAta, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.GameToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(8376924037087523265UL, offset);
                offset += 8;
                _data.WriteU8(raffleType, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction AddToken(AddTokenAccounts accounts, ulong amount, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.VaultPda, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.VaultAta, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerAta, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.GameToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(3766188206372618221UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ReduceToken(ReduceTokenAccounts accounts, ulong amount, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.SignerWallet, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.VaultPda, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.VaultAta, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerAta, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.GameToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15217381811324258862UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}