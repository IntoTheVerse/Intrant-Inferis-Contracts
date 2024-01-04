import * as anchor from "@coral-xyz/anchor";
import { Program } from "@coral-xyz/anchor";
import { IntrantInferis } from "../target/types/intrant_inferis";

describe("intrant-inferis", () =>
{
  var provider = anchor.AnchorProvider.env();
  anchor.setProvider(provider);

  const [playerPDA] = anchor.web3.PublicKey.findProgramAddressSync([Buffer.from("PLAYER"), provider.publicKey.toBuffer()], new anchor.web3.PublicKey("HQrb5QKGh5czu3hC1ahJVJW9DnZRJAs2YxEFGPsPJQop"));
  const program = anchor.workspace.IntrantInferis as Program<IntrantInferis>;
  console.log('program',program);
  
  it("Is initialized!", async () =>
  {
    const tx = await program.methods.initializePlayer("Memxor").accounts({
        signer: provider.publicKey,
        player: playerPDA,
        systemProgram: anchor.web3.SystemProgram.programId
    }).rpc();

    console.log("Your transaction signature", tx);
  });
});