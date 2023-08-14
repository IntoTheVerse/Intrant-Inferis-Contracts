import * as anchor from "@coral-xyz/anchor";
import { Program } from "@coral-xyz/anchor";
import { IntrantInferisSolana } from "../target/types/intrant_inferis_solana";

describe("intrant-inferis-solana", () =>
{
  let provider = anchor.AnchorProvider.env();
  anchor.setProvider(provider);
  
  let nftPublicKey = new anchor.web3.PublicKey("11111111111111111111111111111111");
  //beforeEach(done => setTimeout(done, 5000));
  
  const program = anchor.workspace.IntrantInferisSolana as Program<IntrantInferisSolana>;
  const [player] = anchor.web3.PublicKey.findProgramAddressSync([Buffer.from("PLAYER"), provider.publicKey.toBuffer()], program.programId);
  const [playerCharacter] = anchor.web3.PublicKey.findProgramAddressSync([Buffer.from("PLAYER_CHARACTER"), provider.publicKey.toBuffer(), nftPublicKey.toBuffer()], program.programId);

  it("Initialize Player!", async () =>
  {
    const tx = await program.methods.initializePlayer("Memxor").accounts({player: player}).rpc();
    
    const playerAcc = await program.account.player.fetch(player);
    console.log("Player");
    console.log(playerAcc.username);
    console.log(playerAcc.currentPlayerCharacter);
  });

  it("Initialize Player Character!", async () =>
  {
    const tx = await program.methods.initializePlayerCharacter(nftPublicKey)
      .accounts({ playerCharacterAccount: playerCharacter }).rpc();
    
    const playerCharacterAcc = await program.account.playerCharacter.fetch(playerCharacter);
    console.log("Player Character");
    console.log(playerCharacterAcc.owner);
    console.log(playerCharacterAcc.nftAddress);
    console.log(playerCharacterAcc.locked);
    console.log(playerCharacterAcc.lastLockedTime.toNumber());
  });

  it("Lock Player Character!", async () =>
  {
    const tx = await program.methods.lockPlayerCharacter(nftPublicKey)
      .accounts({ playerCharacterAccount: playerCharacter }).rpc();
    
    const playerCharacterAcc = await program.account.playerCharacter.fetch(playerCharacter);
    console.log("Player Character");
    console.log(playerCharacterAcc.owner);
    console.log(playerCharacterAcc.nftAddress);
    console.log(playerCharacterAcc.locked);
    console.log(playerCharacterAcc.lastLockedTime.toNumber());
  });

  it("Set Current Player Character!", async () =>
  {
    const tx = await program.methods.setCurrentPlayerCharacter(nftPublicKey)
      .accounts({ playerCharacterAccount: playerCharacter, player: player }).rpc();
    
    const playerCharacterAcc = await program.account.playerCharacter.fetch(playerCharacter);
    const playerAcc = await program.account.player.fetch(player);
    console.log("Player");
    console.log(playerAcc.username);
    console.log(playerAcc.currentPlayerCharacter);
    console.log("Player Character");
    console.log(playerCharacterAcc.owner);
    console.log(playerCharacterAcc.nftAddress);
    console.log(playerCharacterAcc.locked);
    console.log(playerCharacterAcc.lastLockedTime.toNumber());
  });
});
