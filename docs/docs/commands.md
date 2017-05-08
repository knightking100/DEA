#Commands
All commands are catagorized by modules. Each of the following sections is a module, and to gain more information about a specific module, you may use the `$help [Module name]` command, or simply read below.

The syntax of the command usage is: 
`Optional paramater: []`
`Required paramater: <>`

##Table Of Contents
- [Bot Owners](#botowners)
- [Owners](#owners)
- [Administration](#administration)
- [Moderation](#moderation)
- [General](#gambling)
- [Gambling](#gambling)
- [Gangs](#gangs)
- [Crime](#crime)
- [Trivia](#trivia)
- [NSFW](#nsfw)
- [Polls](#polls)
- [System](#system)

### BotOwners

These commands may only be used by the bot owners provided by the Owner Ids in the Credentials.json file.

Command | Description | Usage
----------------|--------------|-------
SetGame|Sets the game of DEA.|`$SetGame <Game name>`
LeaveGuild|Leaves any guild by guild ID.|`$LeaveGuild <Guild Id>`
InformOwners|Sends a message to all server owners.|`$InformOwners <Message>`
SendGlobalUpdate|Sends a global update message into all DEA Update channels.|`$SendGlobalUpdate <Message>`
Blacklist|Blacklist a user from DEA entirely to the fullest extent.|`$Blacklist <User Id>`

### Owners

These commands may only be used by a user with the set mod role with a permission level of 3, or the ownership of the server.

Command | Description | Usage
----------------|--------------|-------
AddModRole|Adds a moderator role with different permission levels.|`$AddModRole <@ModRole> <Perm level>`
RemoveModRole|Removes a moderator role.|`$RemoveModRole <@ModRole>`
ModifyModRole|Modifies a moderator role.|`$ModifyModRole <@ModRole> <Perm level>`
ResetCooldowns|Resets all cooldowns for a specific user.|`$ResetCooldowns [@User]`
100k|Sets the user's balance to $100,000.00.|`$100k [@User]`
Add|Add cash into a user's balance.|`$Add <@User> <Cash>]`
Addto|Add cash to every users balance in a specific role.|`$Addto <@Role> <Cash>`
Remove|Remove cash from a user's balance.|`$Remove <@User> <Cash>`
Removefrom|Remove cash to every users balance in a specific role.|`$Removefrom <@Role> <Cash>`
Reset|Resets all user data for the entire server or a specific role.|`$Reset [@Role]`
SetGlobalMultiplier|Sets the global chatting multiplier.|`$SetGlobalMultiplier <Multiplier>`
SetRate|Sets the global temporary multiplier increase rate.|`$SetRate <Increase rate>`

### Administration  

These commands may only be used by a user with the set mod role with a permission level of 2, the Administrator permission.

Command | Description | Usage
----------------|--------------|-------
RoleIds|DMs the user a list of Id's of all roles in the server| `$RoleIds`
SetPrefix|Sets the guild specific prefix.|`$SetPrefix <Prefix>`
SetMutedRole|Sets the muted role.|`$SetMutedRole <@MutedRole>`
AddRank|Adds a rank roles for the DEA cash system.|`$AddRank <@RankRole> <CashRequired>`
RemoveRank|Removes a rank role for the DEA cash system.|`$RemoveRank <@RankRole>`
ModifyRank|Modifies a rank role for the DEA cash system.|`$ModifyRank <@RankRole> <CashRequired>`
SetModLog|Sets the moderation log.|`$SetModLog <#ModLog>`
SetGambleChannel|Sets the gambling channel.|`$SetGambleChannel <#GambleChannel>`

### Moderation 

These commands may only be used by a user with the set mod role with a permission level of 1, or the Administrator permission.

Command | Description | Usage
----------------|--------------|-------
Ban|Bans a user from the server.|`$Ban <@User> [Reason]`
Kick|Kicks a user from the server.|`$Kick <@User> [Reason]`
Mute|Temporarily mutes a user.|`$Mute <@User> [Reason]`
CustomMute|Temporarily mutes a user for x amount of hours.|`$CustomMute <Hours> <@User> [Reason]`
Unmute|Unmutes a muted user.|`$Unmute <@User> [Reason]`
Clear|Deletes x amount of messages.|`$Clear [Quantity of messages] [Reason]`
Chill|Prevents users from talking in for x amount of seconds.| `$chill <Seconds> [Reason]`

### General 
Command | Description | Usage
----------------|--------------|-------
Investments|Increase your money per message|`$Investments [investment]`
Leaderboards|View the richest Drug Traffickers.|`$Leaderboards`
Rates|View the best chatters.|`$Rates`
Donate|Sauce some cash to one of your mates.|`$Donate <@User> <Amount of cash>`
Rank|View the money/message rate of anyone.|`$Rank [@User]`
Money|View the wealth of anyone.|`$Money [@User]`
Rate|View the money/message rate of anyone.|`$Rate [@User]`
Ranks|View all ranks.|`$Ranks`
ModRoles|View all mod roles.|`$ModRoles`
Cooldowns|View all your command cooldowns|`$Cooldowns`

### Gambling 
Command | Description | Usage
----------------|--------------|-------
21+|Roll 20.84 or higher on a 100 sided die, win 0.2X your bet.|`$21+ <Bet>`
50x2|Roll 50.01 or higher on a 100 sided die, win your bet.|`$50x2 <Bet>`
53x2|Roll 52.50 or higher on a 100 sided die, win your bet.|`$53x2 <Bet>`
75+|Roll 75.01 or higher on a 100 sided die, win 2.8X your bet.|`$75+ <Bet>`
100x90|Roll 100.00 on a 100.00 sided die, win 9500X your bet.|`$100x9500 <Bet>`

### Gangs
Command | Description | Usage
----------------|--------------|-------
CreateGang|Allows you to create a gang at a hefty price.|`$CreateGang <Name>`
JoinGang|Sends a request to the gang leader to join their gang.|`$JoinGang <Gang name>`
Gang|Gives you all the info about any gang.|`$Gang [Gang name]`
Ganglb|Shows the wealthiest gangs.|`$Ganglb`
LeaveGang|Allows you to break all ties with a gang.|`$LeaveGang`
KickGangMember|Kicks a user from your gang.|`$KickGangMember <@Member>`
DestroyGang|Destroys a gang entirely taking down all funds with it.|`$DestroyGang`
ChangeGangName|Changes the name of your gang.|`$ChangeGangName <New name>`
Deposit|Deposit cash into your gang's funds.|`$Deposit <Cash>`
Withdraw|Withdraw cash from your gang's funds.|`$Withdraw <Cash>`
Raid|Raid another gang in attempt to steal some of their wealth.| `$Raid <Gang name> <Resources>`

### Crime
Command | Description | Usage
----------------|--------------|-------
Whore|Sell your body for some quick cash.|`$Whore`
Jump|Jump some random nigga in the hood.|`$Jump`
Steal|Snipe some goodies from your local stores.|`$Steal`
Bully|Bully anyone's nickname to whatever you please.|`$Bully`
Rob|Slam anyone's bank account.|`$Rob <Resources>`

### Trivia
Command | Description | Usage
----------------|--------------|-------
ChangeAutoTriviaSettings|Enables/disables the auto trivia feature: Sends a trivia question in the default text channel every 2 minites.|`$ChangeAutoTriviaSettings`
AddQuestion|Adds a trivia question.|`$AddQuestion <Question> <Answer>`
RemoveQuestion|Removes a trivia question.|`$RemoveQuestion <Question>`
ModifyQuestion|Modify a trivia question.|`$ModifyQuestion <Question> <New Question>`
ModifyAnswer|Modify a trivia answer.|`$ModifyAnswer <Question> <New Answer>`
TriviaQuestions|Sends you a list of all trivia questions.|`$TriviaQuestions`
TriviaAnswers|Sends you a list of all trivia answers.|`$TriviaAnswers [Question]`

### NSFW
Command | Description | Usage
----------------|--------------|-------
ChangeNsfwSettings|Enables/disables NSFW commands in your server.|`$ChangeNsfwSettings`
SetNsfwChannel|Information about the DEA Cash System.|`$SetNsfwChannel <@Role>`
Tits|Motorboat that shit.|`$Tits`
Ass|Sauce me some booty how about that.|`$Ass`
Hentai|The real shit goes down with custom hentai tags.|`$Hentai [tag]`

### Polls
Command | Description | Usage
----------------|--------------|-------
CreatePoll|Creates a poll.|`$CreatePoll <poll> <choices> [daysToLast] [elderOnly] [modOnly]`
RemovePoll|Removes a poll.|`$RemovePoll <Poll index>`
Polls|Sends you a list of all polls in progress.|`$Polls`
Poll|View the information of any poll.|`$Poll <Poll index>`
Vote|Vote on any poll.|`$Vote <Poll index> <Choice index>`

### System
Command | Description | Usage
----------------|--------------|-------
Invite|Provides an invite link to add DEA to your Server.|`$Invite`
Cleanup|Deletes DEA's most recent messages to prevent chat flood.|`$Cleanup`
Usage|Explanation of how commands are used.|`$Usage`
Information|Information about the DEA Cash System.|`$Information`
Modules|Provides a list of all Command Modules.|`$Modules`
Help|All command information.|`$Help [Command or Module]`
Stats|All the statistics about DEA.|`$Stats`