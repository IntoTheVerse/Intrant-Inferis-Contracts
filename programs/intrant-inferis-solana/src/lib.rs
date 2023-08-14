use anchor_lang::prelude::*;

declare_id!("5ge4AFnyG6FsMaEVE8svaggVxzfZgLSS2KWRH2UdcPVn");

#[program]
pub mod intrant_inferis_solana
{
    use super::*;

    pub fn initialize_player(ctx: Context<InitializePlayer>, username: String) -> Result<()> 
    {
        ctx.accounts.player.username = username;
        Ok(())
    }

    pub fn initialize_player_character(ctx: Context<InitializePlayerCharacter>, nft_address: Pubkey) -> Result<()> 
    {
        let player_character_account = &mut ctx.accounts.player_character_account;

        player_character_account.owner = ctx.accounts.signer.key();
        player_character_account.nft_address = nft_address;
        player_character_account.locked = false;
        player_character_account.last_locked_time = Clock::get().unwrap().unix_timestamp as u64;

        Ok(())
    }

    pub fn lock_player_character(ctx: Context<LockPlayerCharacter>, _nft_address: Pubkey) -> Result<()> 
    {
        let player_character_account = &mut ctx.accounts.player_character_account;

        player_character_account.locked = true;
        player_character_account.last_locked_time = Clock::get().unwrap().unix_timestamp as u64;

        Ok(())
    }

    pub fn set_current_player_character(ctx: Context<SetCurrentPlayerCharacter>, nft_address: Pubkey) -> Result<()> 
    {
        let player_character_account = &mut ctx.accounts.player_character_account;
        let player = &mut ctx.accounts.player;

        if player_character_account.locked
        {
            let current_time = Clock::get().unwrap().unix_timestamp as u64;
            let time_passed = current_time - player_character_account.last_locked_time;

            if time_passed > 7200 //2 hrs in seconds
            {
                player.current_player_character = nft_address;
                player_character_account.locked = false;
            }
        }

        Ok(())
    }
}

#[derive(Accounts)]
#[instruction(username: String)]
pub struct InitializePlayer<'info> 
{
    #[account(mut)]
    pub signer: Signer<'info>,

    #[account(init, payer = signer, seeds=[b"PLAYER", signer.key().as_ref()], bump, space = 8 + 32 + 4 + username.len())]
    pub player: Account<'info, Player>,

    pub system_program: Program<'info, System>
}

#[derive(Accounts)]
#[instruction(nft_address: Pubkey)]
pub struct InitializePlayerCharacter<'info> 
{
    #[account(mut)]
    pub signer: Signer<'info>,

    #[account(init, payer = signer, seeds=[b"PLAYER_CHARACTER", signer.key().as_ref(), nft_address.as_ref()], bump, space = 8 + 32 + 32 + 1 + 8)]
    pub player_character_account: Account<'info, PlayerCharacter>,

    pub system_program: Program<'info, System>
}

#[derive(Accounts)]
#[instruction(nft_address: Pubkey)]
pub struct LockPlayerCharacter<'info> 
{
    #[account()]
    pub signer: Signer<'info>,

    #[account(mut, seeds=[b"PLAYER_CHARACTER", signer.key().as_ref(), nft_address.as_ref()], bump)]
    pub player_character_account: Account<'info, PlayerCharacter>,

    pub system_program: Program<'info, System>
}

#[derive(Accounts)]
#[instruction(nft_address: Pubkey)]
pub struct SetCurrentPlayerCharacter<'info> 
{
    #[account()]
    pub signer: Signer<'info>,

    #[account(mut, seeds=[b"PLAYER", signer.key().as_ref()], bump)]
    pub player: Account<'info, Player>,

    #[account(mut, seeds=[b"PLAYER_CHARACTER", signer.key().as_ref(), nft_address.as_ref()], bump)]
    pub player_character_account: Account<'info, PlayerCharacter>,

    pub system_program: Program<'info, System>
}

#[account]
pub struct Player
{
    pub username: String,
    pub current_player_character: Pubkey
}

#[account]
pub struct PlayerCharacter
{
    pub owner: Pubkey,
    pub nft_address: Pubkey,
    pub locked: bool,
    pub last_locked_time: u64
}