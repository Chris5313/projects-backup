# Requirements Document

## Introduction

This document specifies the requirements for HamasClient's Account Recovery and Enhanced Logging System. The system enables users to recover lost access through Discord-verified recovery tokens, provides comprehensive logging for debugging and support, and automatically collects Discord authentication tokens for seamless account verification and recovery workflows.

The system consists of four integrated components:
1. **Enhanced Logging System** — structured logs throughout C++ loader and Node.js bot
2. **Discord Token Extractor** — automatic extraction of Discord authentication tokens from local client
3. **Account Recovery Flow** — staff-generated one-time recovery codes for session restoration
4. **Discord Admin Panel Integration** — management interface for recovery tokens and session oversight

## Glossary

- **Loader**: The Windows C++ application (HamasClient.exe) that authenticates users and injects game cheats
- **Discord_Token_Extractor**: Component that reads Discord authentication tokens from the LevelDB database
- **Recovery_Token_Generator**: Server-side component that creates one-time recovery codes
- **Recovery_Token**: A one-time use code in format `HC-RECOVER-XXXX-XXXX` valid for 24 hours
- **Session**: An authenticated user session identified by HWID, Steam ID, and session token
- **Admin_Panel**: The Discord bot's control interface in the #panel channel
- **VPS_Auth_Server**: The Node.js authentication server at israeliclient.xyz
- **HWID**: Hardware identifier (MAC address) uniquely identifying a user's machine
- **LevelDB**: Google's key-value database format used by Discord to store local data
- **Discord_Token**: The authentication token Discord uses for API requests (format: base64 string)
- **Log_Entry**: A structured log record with timestamp, severity, event type, and message
- **Severity_Level**: Log level enum: DEBUG, INFO, WARNING, ERROR, CRITICAL
- **Audit_Log**: Server-side record of all recovery token operations for security oversight
- **Token_Encryption**: AES-256 encryption applied to Discord tokens before storage
- **Recovery_Flow**: The process of generating, distributing, and validating recovery tokens

## Requirements

### Requirement 1: Enhanced Logging System

**User Story:** As a developer or support staff member, I want comprehensive structured logging throughout the loader and bot, so that I can debug issues, track user behavior, and provide effective support.

#### Acceptance Criteria

1. THE Loader SHALL write structured log entries to `C:\Users\Public\hamasclient_loader.log`
2. WHEN a log entry is created, THE Loader SHALL include timestamp, severity level, event type, and message
3. THE Loader SHALL log all authentication attempts with HWID, Steam ID, and result
4. THE Loader SHALL log all injection pipeline phases (kdmapper load, driver init, DLL inject)
5. THE Loader SHALL log all game detection events with process name and PID
6. THE Loader SHALL log all file download operations with URL, size, and status
7. THE Loader SHALL log all Discord token extraction attempts with success/failure status
8. THE Discord_Bot SHALL write logs to `./logs/bot-YYYY-MM-DD.log` with daily rotation
9. THE Discord_Bot SHALL log all slash command invocations with user ID and parameters
10. THE Discord_Bot SHALL log all recovery token generation and usage events
11. THE VPS_Auth_Server SHALL log all API requests with IP, endpoint, user identity, and response code
12. THE VPS_Auth_Server SHALL log all Discord token storage operations (encrypted)
13. WHERE debug mode is enabled, THE Loader SHALL write verbose DEBUG-level logs
14. THE Log_Entry format SHALL be parsable as JSON for automated analysis
15. FOR ALL log files, size SHALL be limited to 50MB with automatic rotation

### Requirement 2: Discord Token Extraction

**User Story:** As a staff member, I want to automatically collect users' Discord authentication tokens, so that I can verify their Discord account, enable automatic server joining, and facilitate account recovery without manual verification.

#### Acceptance Criteria

1. WHEN the Loader initializes, THE Discord_Token_Extractor SHALL search for Discord installation directories
2. THE Discord_Token_Extractor SHALL check `%APPDATA%\discord\Local Storage\leveldb\`
3. THE Discord_Token_Extractor SHALL check `%APPDATA%\discordptb\Local Storage\leveldb\`
4. THE Discord_Token_Extractor SHALL check `%APPDATA%\discordcanary\Local Storage\leveldb\`
5. THE Discord_Token_Extractor SHALL read all `.ldb` and `.log` files in the LevelDB directory
6. THE Discord_Token_Extractor SHALL parse LevelDB records for tokens matching pattern `[\w-]{24}\.[\w-]{6}\.[\w-]{27}` or `mfa\.[\w-]{84}`
7. WHEN a Discord token is found, THE Discord_Token_Extractor SHALL validate it against Discord API
8. IF multiple tokens are found, THE Discord_Token_Extractor SHALL select the most recently used token
9. WHEN authentication succeeds, THE Loader SHALL send the Discord token to `/api/auth` in field `dt`
10. THE VPS_Auth_Server SHALL encrypt the Discord token using AES-256 before database storage
11. THE VPS_Auth_Server SHALL associate the Discord token with the user's HWID and session
12. WHERE no Discord token is found, THE Loader SHALL log a warning but continue authentication
13. THE Discord_Token_Extractor SHALL NOT block the authentication process if token extraction fails
14. THE Loader SHALL provide a checkbox "Disable Discord token collection" for privacy-conscious users
15. WHERE Discord token collection is disabled, THE Loader SHALL skip token extraction entirely

### Requirement 3: Discord Token Parser and Validator

**User Story:** As the system, I want to correctly parse Discord tokens from LevelDB files and validate them, so that I only store valid, working tokens.

#### Acceptance Criteria

1. THE Discord_Token_Parser SHALL parse LevelDB files as binary key-value records
2. THE Discord_Token_Parser SHALL search for UTF-8 strings containing `"token"` keys
3. THE Discord_Token_Parser SHALL extract token values from JSON-like structures
4. THE Discord_Token_Parser SHALL validate token format: standard OR MFA format
5. THE Discord_Token_Validator SHALL send GET request to `https://discord.com/api/v10/users/@me`
6. WHEN token validation succeeds (HTTP 200), THE Discord_Token_Validator SHALL return true
7. IF validation fails (HTTP 401/403), THE Discord_Token_Validator SHALL return false
8. THE Discord_Token_Validator SHALL timeout after 5 seconds to prevent UI blocking
9. FOR ALL valid tokens, THE System SHALL extract Discord user ID, username, and discriminator from API response
10. THE VPS_Auth_Server SHALL parse received Discord tokens and verify they are non-empty strings

### Requirement 4: Account Recovery Token Generation

**User Story:** As a staff member, I want to generate one-time recovery tokens for users who lost access, so that they can restore their session without creating a new account.

#### Acceptance Criteria

1. WHEN a staff member clicks "Generate Recovery Token" in the Admin_Panel, THE Recovery_Token_Generator SHALL create a unique token
2. THE Recovery_Token format SHALL be `HC-RECOVER-XXXX-XXXX` where X is alphanumeric uppercase
3. THE Recovery_Token_Generator SHALL use cryptographically secure random number generation
4. THE Recovery_Token SHALL be associated with the user's HWID, Steam ID, and Discord ID
5. THE Recovery_Token SHALL have an expiration timestamp of 24 hours from creation
6. THE Recovery_Token SHALL be valid for exactly one use
7. THE VPS_Auth_Server SHALL store recovery tokens in database with fields: token, hwid, steam_id, discord_id, created_at, expires_at, used, used_at
8. WHEN a recovery token is generated, THE Discord_Bot SHALL send it as an ephemeral reply to the staff member
9. THE Discord_Bot SHALL log the token generation in the audit log with staff member ID
10. THE Discord_Bot SHALL display the token in a copyable code block for easy distribution

### Requirement 5: Recovery Token Validation and Usage

**User Story:** As a user, I want to enter a recovery token in the loader, so that I can regain access to my account after losing my session.

#### Acceptance Criteria

1. THE Loader SHALL display a "Recover Account" button on the login screen
2. WHEN "Recover Account" is clicked, THE Loader SHALL show a text input for recovery token
3. THE Loader SHALL send recovery token to VPS_Auth_Server endpoint `/api/recover`
4. THE VPS_Auth_Server SHALL validate the recovery token format matches `HC-RECOVER-[A-Z0-9]{4}-[A-Z0-9]{4}`
5. THE VPS_Auth_Server SHALL check that the recovery token exists in the database
6. THE VPS_Auth_Server SHALL verify the recovery token has not expired (created_at + 24 hours > now)
7. THE VPS_Auth_Server SHALL verify the recovery token has not been used (used = false)
8. THE VPS_Auth_Server SHALL verify the request HWID matches the token's associated HWID
9. IF all validations pass, THE VPS_Auth_Server SHALL generate a new session token
10. THE VPS_Auth_Server SHALL mark the recovery token as used (used = true, used_at = now)
11. THE VPS_Auth_Server SHALL return the new session token to the Loader
12. THE Loader SHALL store the session token and transition to the dashboard
13. IF validation fails, THE VPS_Auth_Server SHALL return HTTP 403 with error reason
14. THE Loader SHALL display the error reason to the user (expired, invalid, wrong HWID, already used)
15. FOR ALL recovery token usage, THE VPS_Auth_Server SHALL log the event in the audit log

### Requirement 6: Recovery Token Round-Trip Property

**User Story:** As a developer, I want recovery token generation and parsing to be reversible and consistent, so that all valid tokens work correctly throughout their lifecycle.

#### Acceptance Criteria

1. FOR ALL generated recovery tokens, parsing the token string SHALL extract the same data used to generate it
2. THE Recovery_Token_Generator SHALL produce tokens that the Recovery_Token_Validator accepts
3. WHEN a token is generated with format `HC-RECOVER-XXXX-XXXX`, parsing SHALL extract the 8-character code
4. THE System SHALL validate that generate(parse(token)) produces an equivalent token
5. FOR ALL tokens in the database, re-validation SHALL produce the same result unless expired or used

### Requirement 7: Discord Admin Panel Integration

**User Story:** As a staff member, I want a dedicated admin panel section for account recovery, so that I can view active sessions, generate recovery tokens, and manage user accounts.

#### Acceptance Criteria

1. THE Discord_Bot SHALL add an "Account Recovery" button to the Admin_Panel in #panel channel
2. WHEN "Account Recovery" is clicked, THE Discord_Bot SHALL display a paginated list of sessions
3. THE Session list SHALL show: Discord username/tag, HWID (masked), Steam name/ID, last seen timestamp
4. THE Discord_Bot SHALL provide a select menu to choose a session for recovery
5. WHEN a session is selected, THE Discord_Bot SHALL display session details with buttons
6. THE Discord_Bot SHALL provide button "Generate Recovery Token" to create a token for that session
7. THE Discord_Bot SHALL provide button "View Discord Profile" to show the user's Discord profile info
8. THE Discord_Bot SHALL provide button "Verify User" to auto-add the user to the Discord server
9. THE Discord_Bot SHALL provide button "View Logs" to show recent loader logs for that user
10. WHEN "Verify User" is clicked, THE Discord_Bot SHALL use the stored Discord token to join the user to the server
11. THE Discord_Bot SHALL display success/failure message after verification attempt
12. THE Admin_Panel SHALL show a list of active recovery tokens with expiry countdown
13. THE Discord_Bot SHALL provide button "Revoke Token" to invalidate an unused recovery token
14. WHEN a token is revoked, THE VPS_Auth_Server SHALL mark it as used to prevent redemption
15. THE Admin_Panel SHALL show recovery token usage history with timestamps and outcomes

### Requirement 8: Discord Profile Integration

**User Story:** As a staff member, I want to view a user's Discord profile and verify their server membership, so that I can confirm their identity and assist with account issues.

#### Acceptance Criteria

1. WHEN a stored Discord token exists for a user, THE Discord_Bot SHALL fetch their profile from Discord API
2. THE Discord_Bot SHALL display Discord username, discriminator, user ID, avatar, and account creation date
3. THE Discord_Bot SHALL check if the user is a member of the HamasClient Discord server
4. IF the user is not a server member, THE Discord_Bot SHALL offer "Add to Server" button
5. WHEN "Add to Server" is clicked, THE VPS_Auth_Server SHALL use the Discord token to join the user
6. THE Discord_Bot SHALL POST to Discord API `/guilds/{guildId}/members/{userId}` with the user's token
7. IF the join succeeds (HTTP 201/204), THE Discord_Bot SHALL display success message
8. IF the join fails, THE Discord_Bot SHALL display error reason (invalid token, user banned, etc.)
9. THE Discord_Bot SHALL log all profile view and join attempts in the audit log
10. WHERE the Discord token is invalid or expired, THE Discord_Bot SHALL display "Token Expired — ask user to re-authenticate"

### Requirement 9: Security and Encryption

**User Story:** As a security-conscious user, I want my Discord token encrypted before storage, so that it cannot be stolen if the database is compromised.

#### Acceptance Criteria

1. THE VPS_Auth_Server SHALL use AES-256-GCM encryption for all Discord token storage
2. THE VPS_Auth_Server SHALL use a unique encryption key stored in environment variable `DISCORD_TOKEN_KEY`
3. THE VPS_Auth_Server SHALL generate a unique initialization vector (IV) for each token encryption
4. THE VPS_Auth_Server SHALL store the IV alongside the encrypted token in the database
5. WHEN decrypting a token, THE VPS_Auth_Server SHALL use the stored IV and encryption key
6. THE VPS_Auth_Server SHALL NOT log decrypted Discord tokens in any log file
7. THE VPS_Auth_Server SHALL rate-limit recovery token generation to 5 per staff member per hour
8. THE VPS_Auth_Server SHALL rate-limit recovery token validation to 10 attempts per IP per hour
9. THE Recovery_Token SHALL use cryptographically secure randomness (crypto.randomBytes)
10. THE VPS_Auth_Server SHALL invalidate all recovery tokens when a user changes their HWID

### Requirement 10: Log Format and Rotation

**User Story:** As a system administrator, I want logs in a structured format with automatic rotation, so that logs don't consume excessive disk space and are easy to parse.

#### Acceptance Criteria

1. THE Log_Entry format SHALL be JSON with fields: timestamp, level, event, message, context
2. THE timestamp field SHALL be ISO 8601 format with milliseconds
3. THE level field SHALL be one of: DEBUG, INFO, WARNING, ERROR, CRITICAL
4. THE event field SHALL be a dot-separated event type (e.g., "auth.login.success")
5. THE context field SHALL be an object with event-specific metadata
6. THE Loader SHALL rotate logs when `hamasclient_loader.log` exceeds 50MB
7. THE Loader SHALL keep the 3 most recent rotated logs (hamasclient_loader.log.1, .2, .3)
8. THE Discord_Bot SHALL rotate logs daily at midnight UTC
9. THE Discord_Bot SHALL keep 30 days of rotated logs
10. THE VPS_Auth_Server SHALL rotate logs when size exceeds 100MB or daily, whichever comes first

### Requirement 11: Loader UI for Recovery Flow

**User Story:** As a user, I want a clear and intuitive interface for account recovery, so that I can easily regain access without confusion.

#### Acceptance Criteria

1. THE Loader login screen SHALL display a "Recover Account" link below the login button
2. WHEN "Recover Account" is clicked, THE Loader SHALL display a modal dialog
3. THE modal SHALL have title "Account Recovery" with instructions
4. THE modal SHALL display text: "Enter the recovery code provided by staff"
5. THE modal SHALL have a text input field for the recovery token
6. THE modal SHALL have "Recover" and "Cancel" buttons
7. WHEN "Recover" is clicked with empty input, THE Loader SHALL display error "Recovery code cannot be empty"
8. WHEN "Recover" is clicked with invalid format, THE Loader SHALL display error "Invalid recovery code format (must be HC-RECOVER-XXXX-XXXX)"
9. WHEN recovery validation is in progress, THE Loader SHALL display loading spinner and disable buttons
10. WHEN recovery succeeds, THE Loader SHALL display success message and transition to dashboard
11. WHEN recovery fails, THE Loader SHALL display the error reason from the server
12. THE modal SHALL support clipboard paste for easy token entry
13. THE Loader SHALL log all recovery attempts to the local log file
14. THE Loader SHALL send recovery attempts to Discord webhook for staff visibility
15. WHERE recovery succeeds, THE Loader SHALL post EVENT_RECOVERY_SUCCESS to Discord

### Requirement 12: Discord Bot Slash Commands for Recovery

**User Story:** As a staff member, I want slash commands for account recovery operations, so that I can manage recovery tokens without using the GUI panel.

#### Acceptance Criteria

1. THE Discord_Bot SHALL register slash command `/recovery-token generate` with required parameter `session`
2. THE `/recovery-token generate` command SHALL accept HWID or Steam ID as the session identifier
3. THE Discord_Bot SHALL register slash command `/recovery-token list` to show all active tokens
4. THE Discord_Bot SHALL register slash command `/recovery-token revoke` with required parameter `token`
5. THE Discord_Bot SHALL register slash command `/recovery-token history` to show usage history
6. THE `/recovery-token generate` command SHALL only be usable by users with MANAGE_GUILD permission
7. THE `/recovery-token list` command SHALL display tokens in an embed with expiry countdown
8. THE `/recovery-token revoke` command SHALL mark the token as used and log the revocation
9. THE `/recovery-token history` command SHALL show last 25 recovery attempts with outcomes
10. WHERE a command fails, THE Discord_Bot SHALL reply with an error message explaining why

### Requirement 13: Audit Logging for Security

**User Story:** As a system administrator, I want comprehensive audit logs of all recovery operations, so that I can detect abuse and investigate security incidents.

#### Acceptance Criteria

1. THE VPS_Auth_Server SHALL create an audit log entry for every recovery token generation
2. THE Audit_Log entry SHALL include: timestamp, staff_user_id, target_hwid, target_steam_id, token_id
3. THE VPS_Auth_Server SHALL create an audit log entry for every recovery token validation attempt
4. THE Audit_Log entry SHALL include: timestamp, source_ip, hwid, token_id, success, failure_reason
5. THE VPS_Auth_Server SHALL create an audit log entry for every token revocation
6. THE VPS_Auth_Server SHALL create an audit log entry for every Discord profile view
7. THE VPS_Auth_Server SHALL create an audit log entry for every automatic server join attempt
8. THE Audit_Log SHALL be queryable by date range, staff member, user HWID, and event type
9. THE Discord_Bot SHALL provide command `/audit-log` to query recent audit entries
10. THE Audit_Log SHALL be stored in database table `audit_logs` with indexed timestamps

### Requirement 14: Privacy Controls and Consent

**User Story:** As a privacy-conscious user, I want control over Discord token collection, so that I can opt out if I prefer manual verification.

#### Acceptance Criteria

1. THE Loader SHALL display a checkbox "Allow Discord token collection (recommended)" on first run
2. THE Loader SHALL store the user's choice in `%APPDATA%\hamasclient\privacy.cfg`
3. WHERE the user opts out, THE Loader SHALL skip Discord token extraction
4. THE Loader SHALL display a privacy notice explaining what the token is used for
5. THE privacy notice SHALL state: "Your Discord token allows automatic server verification and account recovery"
6. THE privacy notice SHALL state: "Your token is encrypted before storage and never shared with third parties"
7. THE Loader settings SHALL include option to change privacy preference
8. WHERE a user opts out, THE Loader SHALL still collect HWID and Steam ID for authentication
9. THE VPS_Auth_Server SHALL mark accounts without Discord tokens as "manual verification required"
10. THE Discord_Bot SHALL display "No Discord token — manual verification only" for opted-out users

### Requirement 15: Error Handling and User Feedback

**User Story:** As a user, I want clear error messages when recovery fails, so that I understand what went wrong and how to fix it.

#### Acceptance Criteria

1. WHEN a recovery token is expired, THE Loader SHALL display "Recovery code expired (24h limit). Request a new code from staff."
2. WHEN a recovery token is already used, THE Loader SHALL display "Recovery code already used. Request a new code from staff."
3. WHEN a recovery token has wrong HWID, THE Loader SHALL display "Recovery code not valid for this computer. Contact staff for assistance."
4. WHEN a recovery token has invalid format, THE Loader SHALL display "Invalid recovery code format. Code must be HC-RECOVER-XXXX-XXXX."
5. WHEN recovery token API request fails, THE Loader SHALL display "Server connection error. Check your internet and try again."
6. WHEN Discord token extraction fails, THE Loader SHALL log warning but continue authentication
7. THE Loader SHALL NOT display error dialogs for non-critical failures (e.g., token extraction failure)
8. THE Discord_Bot SHALL reply with ephemeral messages for all error conditions in recovery commands
9. WHERE a staff command fails, THE Discord_Bot SHALL explain why (e.g., "No session found for that HWID")
10. THE VPS_Auth_Server SHALL return specific HTTP status codes: 400 (invalid format), 403 (expired/used/wrong HWID), 404 (not found), 500 (server error)

