use anchor_lang::prelude::*;
use gpl_session::{SessionError, SessionToken, session_auth_or, Session};
use anchor_spl::{token::{Transfer, TokenAccount, Token, Mint}, associated_token::AssociatedToken};

declare_id!("HkXSo3UjiYXEtt7LeiELjdFpAUj7wr8mRQ9px8tBMtCr");

#[program]
pub mod intrant_inferis_solana
{
    use super::*;

    pub fn initialize_player(ctx: Context<InitializePlayer>, username: String) -> Result<()> 
    {
        let player = &mut ctx.accounts.player;
        
        player.username = username;
        player.authority = ctx.accounts.signer.key();
        player.inferis = 0;

        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn initialize_player_character(ctx: Context<InitializePlayerCharacter>, nft_address: Pubkey) -> Result<()> 
    {
        let player_character_account = &mut ctx.accounts.player_character_account;

        player_character_account.owner = ctx.accounts.signer.key();
        player_character_account.nft_address = nft_address;
        player_character_account.locked = false;
        player_character_account.last_locked_time = Clock::get().unwrap().unix_timestamp as u64;

        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn lock_player_character(ctx: Context<LockPlayerCharacter>, _nft_address: Pubkey) -> Result<()> 
    {
        let player_character_account = &mut ctx.accounts.player_character_account;

        player_character_account.locked = true;
        player_character_account.last_locked_time = Clock::get().unwrap().unix_timestamp as u64;

        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
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

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn add_token(ctx: Context<AddToken>, amount: u64) -> Result<()>
    {
        let transfer_accounts = Transfer {
            from: ctx.accounts.vault_ata.to_account_info(),
            to: ctx.accounts.player_ata.to_account_info(),
            authority: ctx.accounts.vault_pda.to_account_info(),
        };

        let seeds:&[&[u8]] = &[
            b"Vault",
            &[*ctx.bumps.get("vault_pda").unwrap()]
        ];
        let signer = &[&seeds[..]];

        let cpi_ctx = CpiContext::new_with_signer(
            ctx.accounts.token_program.to_account_info(),
            transfer_accounts,
            signer
        );

        anchor_spl::token::transfer(cpi_ctx, amount)?;

        ctx.accounts.player.inferis = ctx.accounts.player.inferis + amount;
        Ok(())
    }

    #[session_auth_or(ctx.accounts.player.authority.key() == ctx.accounts.signer.key(), GameErrorCode::WrongAuthority)]
    pub fn reduce_token(ctx: Context<ReduceToken>, amount: u64) -> Result<()>
    {
        let transfer_accounts = Transfer {
            from: ctx.accounts.player_ata.to_account_info(),
            to: ctx.accounts.vault_ata.to_account_info(),
            authority: ctx.accounts.signer_wallet.to_account_info(),
        };

        let cpi_ctx = CpiContext::new(
            ctx.accounts.token_program.to_account_info(),
            transfer_accounts
        );

        anchor_spl::token::transfer(cpi_ctx, amount)?;

        ctx.accounts.player.inferis = ctx.accounts.player.inferis - amount;
        Ok(())
    }
}

#[derive(Accounts)]
#[instruction(username: String)]
pub struct InitializePlayer<'info> 
{
    #[account(mut)]
    pub signer: Signer<'info>,

    #[account(init, payer = signer, seeds=[b"PLAYER", signer.key().as_ref()], bump, space = 8 + 32 + 32 + 8 + 4 + username.len())]
    pub player: Account<'info, Player>,

    pub system_program: Program<'info, System>
}

#[derive(Accounts, Session)]
#[instruction(nft_address: Pubkey)]
pub struct InitializePlayerCharacter<'info> 
{
    #[account(mut)]
    pub signer: Signer<'info>,

    #[account(init, payer = signer, seeds=[b"PLAYER_CHARACTER", signer.key().as_ref(), nft_address.as_ref()], bump, space = 8 + 32 + 32 + 1 + 8)]
    pub player_character_account: Account<'info, PlayerCharacter>,

    #[account(mut, seeds=[b"PLAYER", player.authority.key().as_ref()], bump)]
    pub player: Account<'info, Player>,

    #[session(signer = signer, authority = player.authority.key())]
    pub session_token: Option<Account<'info, SessionToken>>,

    pub system_program: Program<'info, System>
}

#[derive(Accounts, Session)]
#[instruction(nft_address: Pubkey)]
pub struct LockPlayerCharacter<'info> 
{
    #[account()]
    pub signer: Signer<'info>,

    #[account(mut, seeds=[b"PLAYER_CHARACTER", player.authority.key().as_ref(), nft_address.as_ref()], bump)]
    pub player_character_account: Account<'info, PlayerCharacter>,

    #[account(mut, seeds=[b"PLAYER", player.authority.key().as_ref()], bump)]
    pub player: Account<'info, Player>,

    #[session(signer = signer, authority = player.authority.key())]
    pub session_token: Option<Account<'info, SessionToken>>,

    pub system_program: Program<'info, System>
}

#[derive(Accounts, Session)]
#[instruction(nft_address: Pubkey)]
pub struct SetCurrentPlayerCharacter<'info> 
{
    #[account()]
    pub signer: Signer<'info>,

    #[account(mut, seeds=[b"PLAYER", player.authority.key().as_ref()], bump)]
    pub player: Account<'info, Player>,

    #[account(mut, seeds=[b"PLAYER_CHARACTER", player.authority.key().as_ref(), nft_address.as_ref()], bump)]
    pub player_character_account: Account<'info, PlayerCharacter>,

    #[session(signer = signer, authority = player.authority.key())]
    pub session_token: Option<Account<'info, SessionToken>>,

    pub system_program: Program<'info, System>
}

#[derive(Accounts, Session)]
pub struct AddToken<'info>
{
    #[account(mut)]
    pub signer: Signer<'info>,

    #[account(mut, seeds = [b"PLAYER", player.authority.key().as_ref()], bump)]
    pub player: Account<'info, Player>,
  
    ///CHECK:
    #[account(seeds=[b"Vault".as_ref()], bump)]
    pub vault_pda: AccountInfo<'info>,

    #[account(mut, associated_token::mint = game_token, associated_token::authority = vault_pda)]
    pub vault_ata: Account<'info, TokenAccount>,

    #[account(mut, associated_token::mint = game_token, associated_token::authority = player.authority.key())]
    pub player_ata: Account<'info, TokenAccount>,

    pub game_token: Account<'info, Mint>,

    pub token_program: Program<'info, Token>,

    #[session(signer = signer, authority = player.authority.key())]
    pub session_token: Option<Account<'info, SessionToken>>,

    pub associated_token_program: Program<'info, AssociatedToken>,
    pub system_program: Program<'info, System>
}

#[derive(Accounts, Session)]
pub struct ReduceToken<'info>
{
    #[account(mut)]
    pub signer: Signer<'info>,

    #[account(mut)]
    pub signer_wallet: Signer<'info>,

    #[account(mut, seeds = [b"PLAYER", player.authority.key().as_ref()], bump)]
    pub player: Account<'info, Player>,
  
    ///CHECK:
    #[account(seeds=[b"Vault".as_ref()], bump)]
    pub vault_pda: AccountInfo<'info>,

    #[account(mut, associated_token::mint = game_token, associated_token::authority = vault_pda)]
    pub vault_ata: Account<'info, TokenAccount>,

    #[account(mut, associated_token::mint = game_token, associated_token::authority = player.authority.key())]
    pub player_ata: Account<'info, TokenAccount>,

    pub game_token: Account<'info, Mint>,

    pub token_program: Program<'info, Token>,

    #[session(signer = signer, authority = player.authority.key())]
    pub session_token: Option<Account<'info, SessionToken>>,

    pub associated_token_program: Program<'info, AssociatedToken>,
    pub system_program: Program<'info, System>
}

#[account]
pub struct Player
{
    pub username: String,
    pub authority: Pubkey,
    pub current_player_character: Pubkey,
    pub inferis: u64
}

#[account]
pub struct PlayerCharacter
{
    pub owner: Pubkey,
    pub nft_address: Pubkey,
    pub locked: bool,
    pub last_locked_time: u64
}

#[error_code]
pub enum GameErrorCode 
{
    #[msg("Wrong Authority")]
    WrongAuthority,
}