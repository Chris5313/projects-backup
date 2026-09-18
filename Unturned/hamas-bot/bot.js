// ═══════════════════════════════════════════════════════════════════════════
//  HamasClient Discord Bot — full server management
//  Features: tickets, anti-nuke, anti-raid, moderation, keys, logging,
//            welcome, admin commands, auto-role, status updates
// ═══════════════════════════════════════════════════════════════════════════

const {
  Client, GatewayIntentBits, Partials, PermissionFlagsBits,
  EmbedBuilder, ActionRowBuilder, ButtonBuilder, ButtonStyle,
  ChannelType, AuditLogEvent, SlashCommandBuilder, REST, Routes,
  StringSelectMenuBuilder, ModalBuilder, TextInputBuilder, TextInputStyle,
} = require('discord.js');
const fs = require('fs');
const http = require('http');
const path = require('path');

// ── Config ──────────────────────────────────────────────────────────────
const CONFIG = {
  token: process.env.DISCORD_TOKEN || '',
  guildId: '1540199459256795257',
  ownerId: '1539419205198159872',
  color: 0x009736,
  prefix: '!',

  channels: {
    status:       '1540202953414152192',
    rules:        '1540201612767658065',
    announcements:'1540201613862637651',
    updates:      '1540201615200624661',
    faq:          '1540201616110780496',
    download:     '1540201617863741540',
    changelogs:   '1540201618648080495',
    showcase:     '1540201619595993250',
    keys:         '1540201620711940266',
    general:      '1540201624616828978',
    offtopic:     '1540201625325543525',
    media:        '1540201626160209921',
    suggestions:  '1540201627129090218',
    createTicket: '1540201628806811708',
    supportFaq:   '1540201629574369390',
    staffChat:    '1540201631012888676',
    adminCmds:    '1540201631713333349',
    logs:         '1540201632728621187',
    ticketCategory: '1546964576007094363',
    features:     '1540392347911721062',
    // New organized resources channel
    freeStuff:    '1543386290274181264',
    // Source channels (for scraping/monitoring)
    cracking:     '1543059273255231598',
    freestuffUnchecked: '1543053341968310413',
    ai:           '1543063337997176954',
    battleye:     '1543376822274625617',
    // Admin panel (mod-only) — key rotation, user DB, sessions
    panel:        '1543728700296794122',
  },

  roles: {
    owner:      '1540201312376062002',
    admin:      '1540201313084776459',
    mod:        '1540201314129285130',
    dev:        '1540201314946908180',
    support:    '1540201315676725260',
    vip:        '1540201316502999140',
    member:     '1540201316775628862',
    muted:      '1540201318088572999',
    unverified: '1540201319891996714',
  },

  statusMessageId: '1540202954621976617',

  antiNuke: { maxBans: 3, maxKicks: 3, maxChannelDeletes: 2, maxRoleDeletes: 2 },
  antiRaid: { maxJoins: 8, lockdownDuration: 60000 },
};

// ── Auth server admin API (localhost on VPS) ─────────────────────────────
const AUTH_API = {
  base: 'http://127.0.0.1:3999',
  secret: process.env.AUTH_ADMIN_SECRET || 'GW7NG58QLOTKESG76TRMIFH2V5CKLQ66E8Q71VYA6QWO5QQE',
};
function authAPI(method, path, body, timeoutMs = 10000) {
  return new Promise((resolve, reject) => {
    const data = body ? Buffer.from(JSON.stringify(body)) : null;
    const req = http.request(AUTH_API.base + path, {
      method, headers: { 'X-Admin-Secret': AUTH_API.secret, 'Content-Type': 'application/json', ...(data ? { 'Content-Length': data.length } : {}) },
    }, res => { let b = ''; res.on('data', c => b += c); res.on('end', () => { try { resolve({ status: res.statusCode, json: JSON.parse(b || '{}') }); } catch { resolve({ status: res.statusCode, json: null }); } }); });
    req.setTimeout(timeoutMs, () => req.destroy(new Error('auth server timeout')));
    req.on('error', reject);
    if (data) req.write(data);
    req.end();
  });
}

// ── Admin Control Panel (#panel) ─────────────────────────────────────────
const PANEL_DOWNLOAD = 'https://israeliclient.xyz/downloads/HamasClient.exe';
let panelState = {}; // populated after loadJSON/DATA_DIR exist (see below)
function panelRows() {
  return [
    new ActionRowBuilder().addComponents(
      new ButtonBuilder().setCustomId('panel_key').setLabel('Show Key').setEmoji('🔑').setStyle(ButtonStyle.Primary),
      new ButtonBuilder().setCustomId('panel_rotate').setLabel('Rotate Key').setEmoji('♻️').setStyle(ButtonStyle.Secondary),
      new ButtonBuilder().setCustomId('panel_users').setLabel('Users').setEmoji('👥').setStyle(ButtonStyle.Secondary),
      new ButtonBuilder().setCustomId('panel_bans').setLabel('Bans').setEmoji('🚫').setStyle(ButtonStyle.Secondary),
      new ButtonBuilder().setCustomId('panel_sessions').setLabel('Sessions').setEmoji('🟢').setStyle(ButtonStyle.Secondary),
    ),
    new ActionRowBuilder().addComponents(
      new ButtonBuilder().setCustomId('panel_ban').setLabel('Ban…').setEmoji('⛔').setStyle(ButtonStyle.Danger),
      new ButtonBuilder().setCustomId('panel_unban').setLabel('Unban…').setEmoji('✅').setStyle(ButtonStyle.Success),
      new ButtonBuilder().setCustomId('panel_refresh').setLabel('Refresh').setEmoji('🔄').setStyle(ButtonStyle.Secondary),
      new ButtonBuilder().setCustomId('panel_rebuild').setLabel('Rebuild #users').setEmoji('🧹').setStyle(ButtonStyle.Secondary),
      new ButtonBuilder().setLabel('Loader Download').setEmoji('⬇️').setStyle(ButtonStyle.Link).setURL(PANEL_DOWNLOAD),
    ),
  ];
}

function maskKey(k) {
  if (!k) return '?';
  return k.length <= 8 ? k : k.slice(0, 3) + '••••' + k.slice(-2);
}

async function panelEmbed() {
  let usersN = '?', bansN = '?', sessN = '?', key = null, rotatedAt = null;
  try { const r = await authAPI('GET', '/admin/users');    if (r.status === 200) usersN = r.json.count; } catch {}
  try { const r = await authAPI('GET', '/admin/bans');     if (r.status === 200) bansN = r.json.count; } catch {}
  try { const r = await authAPI('GET', '/admin/sessions'); if (r.status === 200) sessN = r.json.count; } catch {}
  try { const r = await authAPI('GET', '/admin/key');      if (r.status === 200) { key = r.json.key; rotatedAt = r.json.rotatedAt; } } catch {}
  const e = new EmbedBuilder()
    .setTitle('🛠️ HamasClient Control Panel')
    .setColor(CONFIG.color)
    .setDescription([
      '**License key:** `' + maskKey(key) + '`' + (rotatedAt ? ' — rotated <t:' + Math.floor(new Date(rotatedAt).getTime() / 1000) + ':R>' : '') + ' *(🔑 reveals the full key)*',
      '',
      '👥 **Users:** `' + usersN + '`  •  🚫 **Bans:** `' + bansN + '`  •  🟢 **Sessions:** `' + sessN + '`',
      '⬇️ **Loader:** ' + PANEL_DOWNLOAD + ' *(v6, VMProtect)*',
      '',
      'Buttons: staff only. Bans/unbans apply instantly.',
    ].join('\n'))
    .setFooter({ text: 'Auto-bans need proof: 8 distinct keys / scripted UA / forged identity / packet probing' })
    .setTimestamp();
  return e;
}

async function postPanel(channel) {
  const embed = await panelEmbed();
  if (panelState.messageId) {
    try { const old = await channel.messages.fetch(panelState.messageId); await old.delete(); } catch {}
  }
  const msg = await channel.send({ embeds: [embed], components: panelRows() });
  panelState.messageId = msg.id;
  saveJSON('panel.json', { messageId: msg.id });
  return msg;
}

async function refreshPanelMessage() {
  try {
    const ch = await client.channels.fetch(CONFIG.channels.panel);
    const msg = await ch.messages.fetch(panelState.messageId);
    await msg.edit({ embeds: [await panelEmbed()], components: panelRows() });
  } catch (e) { console.error('panel refresh err:', e.message); }
}

function isPanelStaff(member) {
  return !!member?.roles?.cache?.hasAny(CONFIG.roles.owner, CONFIG.roles.admin, CONFIG.roles.mod, CONFIG.roles.dev);
}

// Shared renderers for slash commands AND panel buttons
async function renderUsers(page = 0) {
  const r = await authAPI('GET', '/admin/users');
  if (r.status !== 200) return { content: '❌ Auth server error: ' + r.status };
  const list = r.json.users || [];
  if (!list.length) return { content: '💤 No users yet.' };
  const perPage = 10;
  const totalPages = Math.max(1, Math.ceil(list.length / perPage));
  if (page >= totalPages) return { content: '❌ Page ' + (page + 1) + ' does not exist. Total pages: ' + totalPages };
  const slice = list.slice(page * perPage, (page + 1) * perPage);
  const lines = slice.map(u => {
    const f = u.flags > 0 ? ' ⚠️' : '';
    return '`' + u.hwid + '` — [' + u.name + '](' + u.steam + ') — ' + u.sessions + ' sess, ' + u.downloads + ' dl' + f + '\n> <t:' + Math.floor(new Date(u.last_seen).getTime() / 1000) + ':R>';
  }).join('\n');
  const e = new EmbedBuilder()
    .setTitle('👥 User Database — ' + r.json.count + ' users')
    .setColor(CONFIG.color)
    .setDescription(lines)
    .setFooter({ text: 'Page ' + (page + 1) + '/' + totalPages + ' — use /users page:N • ⚠️ = flagged' })
    .setTimestamp();
  return { embeds: [e] };
}

async function renderBans() {
  const r = await authAPI('GET', '/admin/bans');
  if (r.status !== 200) return { content: '❌ Auth server error: ' + r.status };
  const list = r.json.bans || [];
  if (!list.length) return { content: '✅ Ban list is empty.' };
  const lines = list.slice(0, 25).map(b => {
    const who = b.hwid ? '`' + b.hwid + '`' : b.ident ? 'IDENT `' + b.ident + '`' : b.steam ? 'STEAM `' + b.steam + '`' : 'IP `' + (b.ip || '?') + '`';
    const id = b.identity || {};
    return who + ' — ' + b.reason + '\n> ' + (id.u || '?') + ' @ ' + (id.h || '?') + ' • banned <t:' + Math.floor(new Date(b.at).getTime() / 1000) + ':R>' + (b.ip && b.hwid ? ' • IP `' + b.ip + '`' : '');
  }).join('\n');
  const e = new EmbedBuilder()
    .setTitle('⛔ Ban List — ' + r.json.count)
    .setColor(0x992B2B)
    .setDescription(lines.slice(0, 4000))
    .setFooter({ text: 'Bans need proof: 8 distinct keys / scripted UA / forged identity / packet probing • /unban target: to lift' });
  return { embeds: [e] };
}

async function renderSessions() {
  const r = await authAPI('GET', '/admin/sessions');
  if (r.status !== 200) return { content: '❌ Auth server error: ' + r.status };
  const list = r.json.sessions || [];
  if (!list.length) return { content: '💤 No active sessions.' };
  const lines = list.map(s => '`' + s.token + '` — [' + s.name + '](https://steamcommunity.com/profiles/' + s.steam + ') — `' + s.hwid + '`\n> IP `' + s.ip + '` • started <t:' + Math.floor(new Date(s.created).getTime() / 1000) + ':R>').join('\n');
  const e = new EmbedBuilder()
    .setTitle('🟢 Active Sessions — ' + r.json.count)
    .setColor(CONFIG.color)
    .setDescription(lines.slice(0, 4000))
    .setTimestamp();
  return { embeds: [e] };
}

// ── Persistent data ─────────────────────────────────────────────────────
const DATA_DIR = path.join(__dirname, 'data');
if (!fs.existsSync(DATA_DIR)) fs.mkdirSync(DATA_DIR);

function loadJSON(file, fallback = {}) {
  const p = path.join(DATA_DIR, file);
  try { return JSON.parse(fs.readFileSync(p, 'utf8')); } catch { return fallback; }
}
function saveJSON(file, data) {
  fs.writeFileSync(path.join(DATA_DIR, file), JSON.stringify(data, null, 2));
}

let keys = loadJSON('keys.json', { issued: {}, pool: [] });
let warnings = loadJSON('warnings.json', {});
let ticketCount = loadJSON('tickets.json', { count: 0 }).count;
let gameStatus = loadJSON('status.json', {
  unturned:  { status: 'online',  label: 'Unturned' },
  battleye:  { status: 'online',  label: 'Unturned BattlEye Bypass' },
  driver:    { status: 'online',  label: 'Kernel Driver' },
  loader:    { status: 'online',  label: 'Loader' },
  keysys:    { status: 'online',  label: 'Key System' },
  roblox:    { status: 'online',  label: 'Roblox' },
});
panelState = loadJSON('panel.json', {});
let ticketPanelMsgId = loadJSON('ticketPanel.json', {}).messageId;

// ── Feature showcase data (mirrors the in-game menu tabs) ───────────────
const FEATURES = [
  {
    id: 'aimbot', emoji: '🎯', name: 'Aimbot',
    desc: 'Silent aim and hitbox exploitation.',
    features: [
      '**Silent Aimbot** — hit players without looking at them',
      '**Auto Shoot** — automatically fires when target is in FOV',
      '**Straight Raycasting** — precise bullet path calculation',
      '**Melee Silent Aim** — silent aim for melee weapons',
      '**Target Root Bone** — snap to the player root hitbox',
      '**Vehicle Hitbox Exploit** — hit players inside vehicles',
      '**Target Best Hitbox** — auto-picks the best hitbox',
      '**Sphere Hit Points** — advanced hitbox spheres with linecast verification',
      '**Prefer Camera Hit Point** — use camera ray for accuracy',
      '**Hit Distance control** — custom silent aim range',
    ],
  },
  {
    id: 'visuals', emoji: '👁️', name: 'Visuals (ESP)',
    desc: 'ESP for every entity in the game.',
    features: [
      '**Players** — 2D/3D boxes, snaplines, chams, wireframe, text',
      '**Items** — see every item with name + distance',
      '**Vehicles** — ESP with fuel and lock state',
      '**Zombies** — track zombies around you',
      '**Generators** — see generator fuel levels',
      '**Animals** — hunt animals easily',
      '**Beds** — find beds for respawn',
      '**Turrets** — avoid sentry turrets',
      '**Bullets** — see incoming bullets',
      '**Storages** — lockers, crates, and more',
      '**Airdrops** — never miss a drop',
      '**Grenades** — see grenades mid-air',
      '**Ores** — mine with ESP',
      '**2D Box** — with filled/outline variants',
      '**3D Box** — full 3D bounding boxes',
      '**Snaplines** — lines from you to targets',
      '**Chams** — visibility check + custom colors',
      '**Wireframe** — x-ray style rendering',
      '**Render distance limit** — performance control',
    ],
  },
  {
    id: 'misc', emoji: '🔧', name: 'Misc',
    desc: 'Quality-of-life and gameplay tweaks.',
    features: [
      '**No Flash** — remove flashbang effect',
      '**No Grayscale** — remove death grayscale',
      '**No Pain** — remove pain overlay',
      '**No Blur** — remove blur effects',
      '**No Flinch** — no flinch when hit',
      '**No Hallucinations** — remove berry hallucinations',
      '**Raw Walk** — disable smooth walking',
      '**Instant Aiming** — aim instantly with all weapons',
      '**Compass in Inventory** — see compass while in inventory',
      '**Map in Inventory** — see map while in inventory',
      '**All Players on Map** — see every player on your map',
      '**Player Marks on Map** — see player markers',
      '**Disable Scope Blackout** — no blackout around scopes',
      '**Ignore Placement Errors** — build anywhere',
      '**Ignore Leave Timer** — leave servers instantly',
      '**Extended Melee Range** — hit from further away',
      '**Melee Target Box** — 3D red box on melee targets',
      '**Chat Spam** — auto spam with custom text',
      '**Chat on Kill** — custom kill messages',
    ],
  },
  {
    id: 'players', emoji: '👥', name: 'Players',
    desc: 'Player list and detailed player info.',
    features: [
      '**Player List** — every player on the server with search',
      '**Identity** — character name, nickname, Steam ID',
      '**Status** — alive/dead, position, vehicle info',
      '**Equipment** — see what players are wearing',
      '**Group Members** — see who is grouped',
      '**Priority System** — mark players as high/low priority',
    ],
  },
  {
    id: 'world', emoji: '🌍', name: 'World',
    desc: 'Optimization and world rendering.',
    features: [
      '**Master Texture Limit** — control texture quality',
      '**LOD Scaling** — increase/decrease model detail',
      '**Disable Game Particles** — huge FPS boost',
      '**Disable Weapon Tracers** — remove game tracers',
      '**Remove Skybox** — clear the sky',
      '**Weapon Tracers** — custom tracers (GL supported)',
      '**Damage Hitmarkers** — see hits with custom colors',
      '**Walking Tracers** — see where players walked',
      '**Custom Crosshair** — Hamas ☪ crescent+star style included',
      '**Info Panel** — HUD with server/player info',
      '**Player Steps** — footstep markers, circle + crescent styles',
      '**Menu Animation** — smooth menu open',
    ],
  },
  {
    id: 'fov', emoji: '📐', name: 'FOV',
    desc: 'FOV circles for aimbot configuration.',
    features: [
      '**FOV Circles** — draw FOV circles on screen',
      '**Rainbow Fading** — animated rainbow color',
      '**FOV Scaled System** — scale FOV with distance',
      '**Multiple FOV Circles** — different sizes for different uses',
    ],
  },
  {
    id: 'overrides', emoji: '⚙️', name: 'Overrides',
    desc: 'Low-level game overrides.',
    features: [
      '**Method Overrides** — view all hooked game functions',
      '**Toggle Each Override** — enable/disable hooks live',
      '**Auto-Detection** — only workable overrides are shown',
    ],
  },
  {
    id: 'playerfinder', emoji: '🔍', name: 'Player Finder',
    desc: 'Find any player.',
    features: [
      '**Search by Username** — find any player',
      '**Server Tracking** — see what server they are on',
      '**Live Search** — start/stop searching in real time',
    ],
  },
  {
    id: 'skinchanger', emoji: '🧥', name: 'Skin Changer',
    desc: 'Change your skins client-side.',
    features: [
      '**Apply Any Skin** — wear any skin without owning it',
      '**Visible to You** — client-side skin change',
      '**Clear Skins** — reset to default',
    ],
  },
  {
    id: 'keybinds', emoji: '⌨️', name: 'Keybinds',
    desc: 'Bind features to keys.',
    features: [
      '**Bind Any Feature** — hotkey any cheat function',
      '**Multiple Binds** — unlimited keybinds',
      '**Operands** — add conditions to binds',
      '**Easy Management** — add/remove/change binds in-game',
    ],
  },
  {
    id: 'console', emoji: '📜', name: 'Console',
    desc: 'Logs and debugging.',
    features: [
      '**Unity Logs** — view engine logs live',
      '**Unturned Logs** — view game logs',
      '**Client Logs** — view cheat logs',
    ],
  },
  {
    id: 'settings', emoji: '🎨', name: 'Settings',
    desc: 'Full customization.',
    features: [
      '**Color Customization** — change every ESP/menu color',
      '**Rainbow Mode** — animated gradient colors',
      '**Search Colors** — find any color setting instantly',
      '**Config System** — settings save automatically',
    ],
  },
];

function featureEmbed(tab) {
  return new EmbedBuilder()
    .setTitle(`${tab.emoji} ${tab.name}`)
    .setColor(CONFIG.color)
    .setDescription(`*${tab.desc}*\n\n${tab.features.map(f => `• ${f}`).join('\n')}`)
    .setFooter({ text: 'HamasClient • F1 opens the menu in-game' })
    .setTimestamp();
}

function featureMenu() {
  return new ActionRowBuilder().addComponents(
    new StringSelectMenuBuilder()
      .setCustomId('feature_select')
      .setPlaceholder('📂 Select a tab...')
      .addOptions(FEATURES.map(t => ({ label: `${t.emoji} ${t.name}`, value: t.id })))
  );
}

let showcaseMsgId = loadJSON('showcase.json', { messageId: null }).messageId;

// ── Client ──────────────────────────────────────────────────────────────
const client = new Client({
  intents: [
    GatewayIntentBits.Guilds,
    GatewayIntentBits.GuildMembers,
    GatewayIntentBits.GuildMessages,
    GatewayIntentBits.MessageContent,
    GatewayIntentBits.GuildModeration,
  ],
  partials: [Partials.Channel, Partials.Message, Partials.GuildMember],
});

// ── Anti-nuke tracking ──────────────────────────────────────────────────
const actionLog = new Map();

function trackAction(userId, type) {
  if (userId === CONFIG.ownerId || userId === client.user.id) return false;
  if (!actionLog.has(userId)) actionLog.set(userId, []);
  const actions = actionLog.get(userId);
  const now = Date.now();
  actions.push({ type, time: now });
  const recent = actions.filter(a => now - a.time < 10000);
  actionLog.set(userId, recent);
  const counts = {};
  for (const a of recent) counts[a.type] = (counts[a.type] || 0) + 1;
  const limits = { ban: CONFIG.antiNuke.maxBans, kick: CONFIG.antiNuke.maxKicks, channelDelete: CONFIG.antiNuke.maxChannelDeletes, roleDelete: CONFIG.antiNuke.maxRoleDeletes };
  return counts[type] > (limits[type] || 5);
}

async function nukeResponse(guild, userId, action) {
  try {
    const member = await guild.members.fetch(userId).catch(() => null);
    if (member && member.id !== CONFIG.ownerId) {
      await member.roles.set([]).catch(() => {});
      await guild.members.ban(userId, { reason: `Anti-nuke: excessive ${action}` }).catch(() => {});
    }
    await logAction(guild, '🚨 ANTI-NUKE', `<@${userId}> was banned for excessive **${action}** (nuke attempt).`, 0xFF0000);
  } catch (e) { console.error('Anti-nuke error:', e.message); }
}

// ── Anti-raid ───────────────────────────────────────────────────────────
const joinTimes = [];
let raidLockdown = false;

// ── Status update ───────────────────────────────────────────────────────
const STATUS_ICONS = { online: '🟢', updating: '🟡', down: '🔴' };

async function updateStatusEmbed() {
  try {
    const ch = await client.channels.fetch(CONFIG.channels.status);
    if (!ch) return;
    const lines = [
      '> Status indicator meanings:',
      '> 🟢 **Online** — Working & undetected',
      '> 🟡 **Updating** — Maintenance / being updated',
      '> 🔴 **Down** — Detected or temporarily offline',
      '',
      '━━━━━━━━━━━━━━━━━━━━━━━━━━━',
    ];
    // Group by game sections
    const unturnedKeys = ['unturned', 'battleye', 'driver', 'loader', 'keysys'];
    const robloxKeys = ['roblox'];

    lines.push('');
    for (const k of unturnedKeys) {
      const g = gameStatus[k];
      if (g) lines.push(`${STATUS_ICONS[g.status] || '⚪'} **${g.label}** — ${g.status === 'online' ? 'Fully operational' : g.status === 'updating' ? 'Under maintenance' : 'Temporarily offline'}`);
    }
    lines.push('', '━━━━━━━━━━━━━━━━━━━━━━━━━━━', '');
    for (const k of robloxKeys) {
      const g = gameStatus[k];
      if (g) lines.push(`${STATUS_ICONS[g.status] || '⚪'} **${g.label}** — ${g.status === 'online' ? 'Fully operational' : g.status === 'updating' ? 'Under maintenance' : 'Temporarily offline'}`);
    }
    lines.push('', '━━━━━━━━━━━━━━━━━━━━━━━━━━━');

    const embed = new EmbedBuilder()
      .setTitle('📡 HamasClient — Live Status')
      .setColor(CONFIG.color)
      .setDescription(lines.join('\n'))
      .setFooter({ text: 'Last updated' })
      .setTimestamp();

    await ch.messages.edit(CONFIG.statusMessageId, { embeds: [embed] });
  } catch (e) { console.error('Status update error:', e.message); }
}

// ── Logging ─────────────────────────────────────────────────────────────
async function logAction(guild, title, description, color = CONFIG.color) {
  try {
    const ch = guild.channels.cache.get(CONFIG.channels.logs);
    if (!ch) return;
    await ch.send({ embeds: [new EmbedBuilder().setTitle(title).setDescription(description).setColor(color).setTimestamp()] });
  } catch (e) { console.error('Log error:', e.message); }
}

// ═══ EVENTS ═════════════════════════════════════════════════════════════

client.once('ready', async () => {
  console.log(`✓ Bot online: ${client.user.tag}`);
  client.user.setActivity('HamasClient | !help', { type: 3 });

  const rest = new REST().setToken(CONFIG.token);
  const commands = [
    new SlashCommandBuilder().setName('help').setDescription('Show bot commands'),
    new SlashCommandBuilder().setName('ping').setDescription('Check bot latency'),
    new SlashCommandBuilder().setName('serverinfo').setDescription('Show server information'),
    new SlashCommandBuilder().setName('userinfo').setDescription('Show user information')
      .addUserOption(o => o.setName('user').setDescription('Target user')),
    new SlashCommandBuilder().setName('warn').setDescription('Warn a user')
      .addUserOption(o => o.setName('user').setDescription('User to warn').setRequired(true))
      .addStringOption(o => o.setName('reason').setDescription('Reason').setRequired(true))
      .setDefaultMemberPermissions(PermissionFlagsBits.ModerateMembers),
    new SlashCommandBuilder().setName('mute').setDescription('Mute a user')
      .addUserOption(o => o.setName('user').setDescription('User to mute').setRequired(true))
      .addStringOption(o => o.setName('reason').setDescription('Reason'))
      .setDefaultMemberPermissions(PermissionFlagsBits.ModerateMembers),
    new SlashCommandBuilder().setName('unmute').setDescription('Unmute a user')
      .addUserOption(o => o.setName('user').setDescription('User to unmute').setRequired(true))
      .setDefaultMemberPermissions(PermissionFlagsBits.ModerateMembers),
    new SlashCommandBuilder().setName('purge').setDescription('Delete messages')
      .addIntegerOption(o => o.setName('count').setDescription('Number of messages (1-100)').setRequired(true).setMinValue(1).setMaxValue(100))
      .setDefaultMemberPermissions(PermissionFlagsBits.ManageMessages),
    new SlashCommandBuilder().setName('nuke').setDescription('Delete and recreate this channel')
      .setDefaultMemberPermissions(PermissionFlagsBits.ManageChannels),
    new SlashCommandBuilder().setName('wipe').setDescription('Delete messages in this channel')
      .addIntegerOption(o => o.setName('count').setDescription('Number of messages to delete').setRequired(true).setMinValue(1).setMaxValue(500))
      .setDefaultMemberPermissions(PermissionFlagsBits.ManageMessages),
    new SlashCommandBuilder().setName('ban').setDescription('Ban a user')
      .addUserOption(o => o.setName('user').setDescription('User to ban').setRequired(true))
      .addStringOption(o => o.setName('reason').setDescription('Reason'))
      .setDefaultMemberPermissions(PermissionFlagsBits.BanMembers),
    new SlashCommandBuilder().setName('kick').setDescription('Kick a user')
      .addUserOption(o => o.setName('user').setDescription('User to kick').setRequired(true))
      .addStringOption(o => o.setName('reason').setDescription('Reason'))
      .setDefaultMemberPermissions(PermissionFlagsBits.KickMembers),
    new SlashCommandBuilder().setName('announce').setDescription('Post an announcement')
      .addStringOption(o => o.setName('title').setDescription('Title').setRequired(true))
      .addStringOption(o => o.setName('message').setDescription('Message').setRequired(true))
      .setDefaultMemberPermissions(PermissionFlagsBits.Administrator),
    new SlashCommandBuilder().setName('warnings').setDescription('Check warnings for a user')
      .addUserOption(o => o.setName('user').setDescription('User to check').setRequired(true))
      .setDefaultMemberPermissions(PermissionFlagsBits.ModerateMembers),
    new SlashCommandBuilder().setName('lockdown').setDescription('Toggle channel lockdown')
      .setDefaultMemberPermissions(PermissionFlagsBits.ManageChannels),
    new SlashCommandBuilder().setName('slowmode').setDescription('Set slowmode')
      .addIntegerOption(o => o.setName('seconds').setDescription('Seconds (0 = off)').setRequired(true).setMinValue(0).setMaxValue(21600))
      .setDefaultMemberPermissions(PermissionFlagsBits.ManageChannels),
    new SlashCommandBuilder().setName('setstatus').setDescription('Set game status')
      .addStringOption(o => o.setName('game').setDescription('Game key').setRequired(true)
        .addChoices(
          { name: 'Unturned', value: 'unturned' },
          { name: 'BattlEye Bypass', value: 'battleye' },
          { name: 'Kernel Driver', value: 'driver' },
          { name: 'Loader', value: 'loader' },
          { name: 'Key System', value: 'keysys' },
          { name: 'Roblox', value: 'roblox' },
        ))
      .addStringOption(o => o.setName('status').setDescription('Status').setRequired(true)
        .addChoices(
          { name: '🟢 Online', value: 'online' },
          { name: '🟡 Updating', value: 'updating' },
          { name: '🔴 Down', value: 'down' },
        ))
      .setDefaultMemberPermissions(PermissionFlagsBits.Administrator),
    // ── Admin Panel commands (mod+ via default perms) ──
    new SlashCommandBuilder().setName('key').setDescription('Show or rotate the license key')
      .addSubcommand(sc => sc.setName('show').setDescription('Show the current license key'))
      .addSubcommand(sc => sc.setName('rotate').setDescription('Rotate to a new random key')
        .addStringOption(o => o.setName('custom').setDescription('Custom key (8-64 chars A-Z 0-9 -)').setRequired(false)))
      .setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild),
    new SlashCommandBuilder().setName('users').setDescription('List user database')
      .addIntegerOption(o => o.setName('page').setDescription('Page number').setRequired(false).setMinValue(1).setMaxValue(50))
      .setDefaultMemberPermissions(PermissionFlagsBits.ModerateMembers),
    new SlashCommandBuilder().setName('user').setDescription('Show full user profile')
      .addStringOption(o => o.setName('hwid').setDescription('HWID (MAC format AA:BB:CC:DD:EE:FF)').setRequired(true))
      .setDefaultMemberPermissions(PermissionFlagsBits.ModerateMembers),
    new SlashCommandBuilder().setName('deluser').setDescription('Delete a user from the database')
      .addStringOption(o => o.setName('hwid').setDescription('HWID to delete').setRequired(true))
      .setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild),
    new SlashCommandBuilder().setName('sessions').setDescription('Show active sessions')
      .setDefaultMemberPermissions(PermissionFlagsBits.ModerateMembers),
    new SlashCommandBuilder().setName('bans').setDescription('Show the ban list (auto-banned crackers)')
      .setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild),
    new SlashCommandBuilder().setName('unban').setDescription('Remove a HWID or IP from the ban list')
      .addStringOption(o => o.setName('target').setDescription('HWID (AA:BB:CC:DD:EE:FF) or IP to unban').setRequired(true))
      .setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild),
    new SlashCommandBuilder().setName('panel').setDescription('Repost the admin control panel in #panel')
      .setDefaultMemberPermissions(PermissionFlagsBits.ManageGuild),
  ].map(c => c.toJSON());

  try {
    await rest.put(Routes.applicationGuildCommands(client.user.id, CONFIG.guildId), { body: commands });
    console.log('✓ Slash commands registered');
  } catch (e) { console.error('Slash command error:', e.message); }

  // ── Admin control panel message (#panel) ──
  try {
    const panelCh = await client.channels.fetch(CONFIG.channels.panel).catch(() => null);
    if (panelCh) {
      await postPanel(panelCh);
      console.log('✓ Admin panel ready');
    }
  } catch (e) { console.error('Panel error:', e.message); }

  // ── Feature showcase message ──
  try {
    const featCh = await client.channels.fetch(CONFIG.channels.features).catch(() => null);
    if (featCh) {
      const mainEmbed = new EmbedBuilder()
        .setTitle('🖥️ HAMASCLIENT — Feature Showcase')
        .setColor(CONFIG.color)
        .setDescription([
          'Welcome to the **full feature list** of HamasClient — the free Unturned cheat.',
          '',
          '📂 **Pick a tab from the dropdown below** to browse features, just like the in-game menu.',
          '',
          '> 🔑 Key: `al-qassam-brigade`',
          '> 📥 Download: <#1540201617863741540>',
          '> 🎫 Support: <#1540201628806811708>',
        ].join('\n'))
        .setFooter({ text: 'HamasClient • Free forever' })
        .setTimestamp();
      const components = [featureMenu()];
      if (showcaseMsgId) {
        await featCh.messages.edit(showcaseMsgId, { embeds: [mainEmbed], components }).catch(() => { showcaseMsgId = null; });
      }
      if (!showcaseMsgId) {
        const msg = await featCh.send({ embeds: [mainEmbed], components });
        showcaseMsgId = msg.id;
        saveJSON('showcase.json', { messageId: msg.id });
      }
      console.log('✓ Feature showcase ready');
    }
  } catch (e) { console.error('Showcase error:', e.message); }

  // ── Ticket panel message (#create-ticket) ──
  try {
    const tickCh = await client.channels.fetch(CONFIG.channels.createTicket).catch(() => null);
    if (tickCh) {
      const embed = new EmbedBuilder()
        .setTitle('🎫 HamasClient Support')
        .setColor(CONFIG.color)
        .setDescription([
          '**Need help with the client, key, or loader?**',
          '',
          'Click the button below to open a private ticket with staff.',
          '',
          '> 🔑 Key: `al-qassam-brigade`',
          '> 📥 Download: <#1540201617863741540>',
          '> 📖 FAQ: <#1540201616110780496>',
        ].join('\n'))
        .setFooter({ text: 'HamasClient • Support' })
        .setTimestamp();
      const components = [new ActionRowBuilder().addComponents(
        new ButtonBuilder().setCustomId('create_ticket').setLabel('Create Ticket').setEmoji('🎫').setStyle(ButtonStyle.Primary))];
      if (ticketPanelMsgId) {
        await tickCh.messages.edit(ticketPanelMsgId, { embeds: [embed], components }).catch(() => { ticketPanelMsgId = null; });
      }
      if (!ticketPanelMsgId) {
        // remove stale duplicates, then post fresh
        const old = await tickCh.messages.fetch({ limit: 20 }).catch(() => null);
        if (old) for (const m of old.values()) { if (m.author.id === client.user.id) await m.delete().catch(() => {}); }
        const msg = await tickCh.send({ embeds: [embed], components });
        ticketPanelMsgId = msg.id;
        saveJSON('ticketPanel.json', { messageId: msg.id });
      }
      console.log('✓ Ticket panel ready');
    }
  } catch (e) { console.error('Ticket panel error:', e.message); }
});

// ── Member join ─────────────────────────────────────────────────────────
client.on('guildMemberAdd', async (member) => {
  if (member.guild.id !== CONFIG.guildId) return;

  // joinTimes.push(Date.now());
  // const now = Date.now();
  // const recent = joinTimes.filter(t => now - t < 10000);
  // if (recent.length >= CONFIG.antiRaid.maxJoins && !raidLockdown) {
  //   raidLockdown = true;
  //   await logAction(member.guild, '🚨 ANTI-RAID', `Raid detected! ${CONFIG.antiRaid.maxJoins}+ joins in 10s. Lockdown activated.`, 0xFF0000);
  //   await member.guild.setVerificationLevel(4).catch(() => {});
  //   setTimeout(async () => {
  //     raidLockdown = false;
  //     await member.guild.setVerificationLevel(1).catch(() => {});
  //     await logAction(member.guild, '✅ RAID OVER', 'Lockdown lifted.', CONFIG.color);
  //   }, CONFIG.antiRaid.lockdownDuration);
  // }

  await member.roles.add(CONFIG.roles.member).catch(() => {});

  try {
    await member.send({ embeds: [new EmbedBuilder()
      .setTitle('Welcome to HamasClient! 🎮')
      .setColor(CONFIG.color)
      .setDescription(`Hey **${member.user.username}**, welcome!\n\n📜 Read the rules in <#${CONFIG.channels.rules}>\n📥 Download in <#${CONFIG.channels.download}>\n🔑 Key: \`al-qassam-brigade\` (in <#${CONFIG.channels.keys}>)\n🎫 Need help? <#${CONFIG.channels.createTicket}>\n\nEnjoy! 🇵🇸`)
      .setTimestamp()] });
  } catch {}

  await logAction(member.guild, '📥 Member Joined', `<@${member.id}> (${member.user.tag})\nAccount created: <t:${Math.floor(member.user.createdTimestamp / 1000)}:R>`, 0x2ECC71);
});

client.on('guildMemberRemove', async (member) => {
  if (member.guild.id !== CONFIG.guildId) return;
  await logAction(member.guild, '📤 Member Left', `**${member.user.tag}** (${member.id})`, 0xE74C3C);
});

// ── Anti-nuke audit log ─────────────────────────────────────────────────
client.on('guildBanAdd', async (ban) => {
  if (ban.guild.id !== CONFIG.guildId) return;
  try {
    const logs = await ban.guild.fetchAuditLogs({ type: AuditLogEvent.MemberBanAdd, limit: 1 });
    const entry = logs.entries.first();
    if (entry && entry.executor.id !== client.user.id && trackAction(entry.executor.id, 'ban'))
      await nukeResponse(ban.guild, entry.executor.id, 'bans');
  } catch {}
});

client.on('channelDelete', async (channel) => {
  if (channel.guild?.id !== CONFIG.guildId) return;
  try {
    const logs = await channel.guild.fetchAuditLogs({ type: AuditLogEvent.ChannelDelete, limit: 1 });
    const entry = logs.entries.first();
    if (entry && entry.executor.id !== client.user.id && entry.executor.id !== CONFIG.ownerId && trackAction(entry.executor.id, 'channelDelete'))
      await nukeResponse(channel.guild, entry.executor.id, 'channel deletions');
  } catch {}
});

client.on('roleDelete', async (role) => {
  if (role.guild.id !== CONFIG.guildId) return;
  try {
    const logs = await role.guild.fetchAuditLogs({ type: AuditLogEvent.RoleDelete, limit: 1 });
    const entry = logs.entries.first();
    if (entry && entry.executor.id !== client.user.id && entry.executor.id !== CONFIG.ownerId && trackAction(entry.executor.id, 'roleDelete'))
      await nukeResponse(role.guild, entry.executor.id, 'role deletions');
  } catch {}
});

// ── Interaction handler ─────────────────────────────────────────────────
client.on('interactionCreate', async (interaction) => {
  // ═══ Admin Panel — modals ═══
  if (interaction.isModalSubmit() && (interaction.customId === 'panel_ban_modal' || interaction.customId === 'panel_unban_modal')) {
    if (!isPanelStaff(interaction.member)) return interaction.reply({ content: '❌ Staff only.', flags: 64 });
    // Normalize: strip spaces, user@host → user|host, so any paste format works
    let target = (interaction.fields.getTextInputValue('panel_target') || '').trim().replace(/\s+/g, ' ');
    if (/^(\S+)\s*@\s*(\S+)$/.test(target)) target = target.replace(/^(\S+)\s*@\s*(\S+)$/, '$1|$2').toLowerCase();
    if (/^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$/.test(target)) target = target.toUpperCase();
    if (!target) return interaction.reply({ content: '❌ Empty target.', flags: 64 });
    await interaction.deferReply({ flags: 64 });
    try {
      if (interaction.customId === 'panel_unban_modal') {
        const r = await authAPI('POST', '/admin/unban', { target });
        if (r.status !== 200) return interaction.editReply({ content: '❌ Unban failed: ' + r.status });
        if (!r.json.removed) return interaction.editReply({ content: '❌ No ban found for `' + target + '`.' });
        await interaction.editReply({ content: '✅ Removed ' + r.json.removed + ' ban(s) for `' + target + '`. They can auth again.' });
        await logAction(interaction.guild, '⛔→✅ Unbanned', '`' + target + '` by <@' + interaction.user.id + '> (panel)', 0x2ECC71);
      } else {
        const reason = (interaction.fields.getTextInputValue('panel_reason') || '').trim() || 'manual ban';
        // Auto-detect target type: HWID / SteamID / ident (a|b) / IP or prefix (23.234.*)
        let body = { reason };
        if (/^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$/.test(target)) body.hwid = target;
        else if (/^\d{17}$/.test(target)) body.steam = target;
        else if (target.includes('|')) body.ident = target;
        else if (/^[0-9a-fA-F.:\*]{2,45}$/.test(target)) body.ip = target;
        else return interaction.editReply({ content: '❌ Unrecognized target. Use a HWID (`AA:BB:CC:DD:EE:FF`), IP or prefix (`23.234.*`), SteamID64, or identity (`user|host`).' });
        const r = await authAPI('POST', '/admin/ban', body);
        if (r.status !== 200) return interaction.editReply({ content: '❌ Ban failed: ' + r.status + (r.json?.error ? ' — ' + r.json.error : '') });
        if (!r.json.added) return interaction.editReply({ content: '⚠️ `' + target + '` is already banned (or covered by an existing ban).' });
        await interaction.editReply({ content: '⛔ Banned `' + target + '` — `' + reason + '`. Refused silently on next attempt.' });
        await logAction(interaction.guild, '⛔ Manual Ban', '`' + target + '` — ' + reason + ' by <@' + interaction.user.id + '> (panel)', 0x992B2B);
      }
      await refreshPanelMessage();
    } catch (e) { return interaction.editReply({ content: '❌ ' + e.message }); }
    return;
  }

  // ═══ Admin Panel — buttons ═══
  if (interaction.isButton() && interaction.customId.startsWith('panel_') && interaction.customId !== 'panel_ban' && interaction.customId !== 'panel_unban') {
    if (!isPanelStaff(interaction.member)) return interaction.reply({ content: '❌ Staff only.', flags: 64 });
    const id = interaction.customId;
    try {
      if (id === 'panel_refresh') {
        return await interaction.update({ embeds: [await panelEmbed()], components: panelRows() });
      }
      await interaction.deferReply({ flags: 64 });
      if (id === 'panel_key') {
        const r = await authAPI('GET', '/admin/key');
        if (r.status !== 200) return interaction.editReply({ content: '❌ Auth server error: ' + r.status });
        return interaction.editReply({ content: '🔑 Current license key: `' + r.json.key + '`' + (r.json.rotatedAt ? '\nrotated <t:' + Math.floor(new Date(r.json.rotatedAt).getTime() / 1000) + ':R>' : '') });
      }
      if (id === 'panel_rotate') {
        return interaction.editReply({
          content: '⚠️ **Rotate the license key?**\n> • Everyone must enter the new key\n> • All active sessions are invalidated\n> • The old key stops working instantly',
          components: [new ActionRowBuilder().addComponents(
            new ButtonBuilder().setCustomId('panel_rotate_go').setLabel('Confirm Rotate').setEmoji('♻️').setStyle(ButtonStyle.Danger))],
        });
      }
      if (id === 'panel_rotate_go') {
        const r = await authAPI('POST', '/admin/key', {});
        if (r.status !== 200) return interaction.editReply({ content: '❌ Rotation failed: ' + r.status, components: [] });
        await interaction.editReply({ content: '🔄 **Key rotated!**\n\n🔑 New key: `' + r.json.key + '`\n\nAll sessions invalidated. The new key only exists server-side now — crackers with the old one are locked out.', components: [] });
        await logAction(interaction.guild, '🔑 Key Rotated', 'New key: `' + r.json.key + '` by <@' + interaction.user.id + '> (panel)', 0xF39C12);
        await refreshPanelMessage();
        return;
      }
      if (id === 'panel_users')   return interaction.editReply(await renderUsers(0));
      if (id === 'panel_bans')    return interaction.editReply(await renderBans());
      if (id === 'panel_sessions') return interaction.editReply(await renderSessions());
      if (id === 'panel_rebuild') {
        const r = await authAPI('POST', '/admin/rebuild-users', {}, 60000);
        if (r.status !== 200) return interaction.editReply({ content: '❌ Rebuild failed: ' + r.status });
        return interaction.editReply({ content: '🧹 **Rebuild started.** Purging #users and re-posting one card per user (~2-3 min, rate-limited). Check #users when done.' });
      }
    } catch (e) { return interaction.editReply({ content: '❌ ' + e.message }).catch(() => {}); }
  }

  // Panel buttons that open modals
  if (interaction.isButton() && (interaction.customId === 'panel_ban' || interaction.customId === 'panel_unban')) {
    if (!isPanelStaff(interaction.member)) return interaction.reply({ content: '❌ Staff only.', flags: 64 });
    const banning = interaction.customId === 'panel_ban';
    try {
      const modal = new ModalBuilder()
        .setCustomId(banning ? 'panel_ban_modal' : 'panel_unban_modal')
        .setTitle(banning ? 'Manual Ban' : 'Remove Ban');
      modal.addComponents(new ActionRowBuilder().addComponents(
        new TextInputBuilder().setCustomId('panel_target')
          .setLabel(banning ? 'Target — HWID / IP / SteamID / user|host' : 'Target to unban')  // ≤45 char Discord limit
          .setStyle(TextInputStyle.Short).setRequired(true)
          .setPlaceholder(banning ? 'AA:BB:CC:DD:EE:FF or 23.234.* or 76561198… or user|host' : 'AA:BB:CC:DD:EE:FF or IP or user|host')));
      if (banning) modal.addComponents(new ActionRowBuilder().addComponents(
        new TextInputBuilder().setCustomId('panel_reason')
          .setLabel('Reason (shown in /bans)')
          .setStyle(TextInputStyle.Short).setRequired(false)
          .setPlaceholder('e.g. selling leaked loader')));
      return await interaction.showModal(modal);
    } catch (e) {
      return interaction.reply({ content: '❌ Modal failed: ' + e.message, flags: 64 }).catch(() => {});
    }
  }

  // ── Feature showcase select menu ──
  if (interaction.isStringSelectMenu() && interaction.customId === 'feature_select') {
    const tab = FEATURES.find(t => t.id === interaction.values[0]);
    if (!tab) return interaction.reply({ content: '❌ Unknown tab.', flags: 64 });
    try {
      await interaction.update({ embeds: [featureEmbed(tab)], components: [featureMenu()] });
    } catch {
      await interaction.reply({ embeds: [featureEmbed(tab)], components: [featureMenu()], flags: 64 });
    }
    return;
  }

  // ── Ticket buttons ──
  if (interaction.isButton()) {
    if (interaction.customId === 'create_ticket') {
      ticketCount++;
      saveJSON('tickets.json', { count: ticketCount });
      const ticketName = `ticket-${ticketCount.toString().padStart(4, '0')}`;
      try {
        const ch = await interaction.guild.channels.create({
          name: ticketName, type: ChannelType.GuildText, parent: CONFIG.channels.ticketCategory,
          permissionOverwrites: [
            { id: interaction.guild.id, deny: [PermissionFlagsBits.ViewChannel] },
            { id: interaction.user.id, allow: [PermissionFlagsBits.ViewChannel, PermissionFlagsBits.SendMessages, PermissionFlagsBits.ReadMessageHistory, PermissionFlagsBits.AttachFiles] },
            { id: CONFIG.roles.support, allow: [PermissionFlagsBits.ViewChannel, PermissionFlagsBits.SendMessages, PermissionFlagsBits.ReadMessageHistory, PermissionFlagsBits.ManageMessages] },
            { id: CONFIG.roles.mod, allow: [PermissionFlagsBits.ViewChannel, PermissionFlagsBits.SendMessages, PermissionFlagsBits.ReadMessageHistory, PermissionFlagsBits.ManageMessages] },
            { id: CONFIG.roles.admin, allow: [PermissionFlagsBits.ViewChannel, PermissionFlagsBits.SendMessages, PermissionFlagsBits.ReadMessageHistory, PermissionFlagsBits.ManageMessages] },
          ],
        });
        await ch.send({
          content: `<@${interaction.user.id}>`,
          embeds: [new EmbedBuilder().setTitle(`🎫 Ticket #${ticketCount}`).setColor(CONFIG.color)
            .setDescription(`Welcome <@${interaction.user.id}>!\n\nDescribe your issue and staff will help shortly.\n\n**Include:**\n> • What went wrong\n> • Error messages / screenshots\n> • What you've tried`)
            .setTimestamp()],
          components: [new ActionRowBuilder().addComponents(
            new ButtonBuilder().setCustomId('close_ticket').setLabel('🔒 Close Ticket').setStyle(ButtonStyle.Danger),
            new ButtonBuilder().setCustomId('claim_ticket').setLabel('📋 Claim').setStyle(ButtonStyle.Primary))]
        });
        await interaction.reply({ content: `Ticket created: <#${ch.id}>`, flags: 64 });
        await logAction(interaction.guild, '🎫 Ticket Created', `**${ticketName}** by <@${interaction.user.id}>`, CONFIG.color);
      } catch (e) {
        await interaction.reply({ content: '❌ Failed to create ticket.', flags: 64 });
      }
      return;
    }
    if (interaction.customId === 'close_ticket') {
      if (!interaction.channel.name.startsWith('ticket-')) return;
      await interaction.reply({ embeds: [new EmbedBuilder().setTitle('🔒 Closing...').setDescription('Deleting in 5 seconds.').setColor(0xFF0000)] });
      await logAction(interaction.guild, '🎫 Ticket Closed', `**${interaction.channel.name}** closed by <@${interaction.user.id}>`, 0xE74C3C);
      setTimeout(() => interaction.channel.delete().catch(() => {}), 5000);
      return;
    }
    if (interaction.customId === 'claim_ticket') {
      const isStaff = interaction.member.roles.cache.hasAny(CONFIG.roles.support, CONFIG.roles.mod, CONFIG.roles.admin, CONFIG.roles.owner);
      if (!isStaff) return interaction.reply({ content: '❌ Staff only.', flags: 64 });
      await interaction.reply({ embeds: [new EmbedBuilder().setDescription(`📋 Claimed by <@${interaction.user.id}>`).setColor(CONFIG.color)] });
      return;
    }
    return;
  }

  if (!interaction.isChatInputCommand()) return;
  const { commandName } = interaction;

  if (commandName === 'ping') {
    return interaction.reply({ embeds: [new EmbedBuilder().setTitle('🏓 Pong!').setColor(CONFIG.color)
      .setDescription(`Latency: **${Date.now() - interaction.createdTimestamp}ms**\nAPI: **${client.ws.ping}ms**`)] });
  }

  if (commandName === 'help') {
    return interaction.reply({ embeds: [new EmbedBuilder().setTitle('📖 HamasClient Bot Commands').setColor(CONFIG.color)
      .addFields(
        { name: '🎮 General', value: '`/ping` — Latency\n`/help` — Commands\n`/serverinfo` — Stats\n`/userinfo` — User info', inline: true },
        { name: '🔨 Moderation', value: '`/warn` `/mute` `/unmute` `/kick` `/ban` `/purge` `/wipe` `/warnings`', inline: true },
        { name: '⚙️ Admin', value: '`/announce` `/nuke` `/lockdown` `/slowmode` `/setstatus`', inline: true },
        { name: '🛡️ Panel', value: '`/panel` — repost the #panel control panel\n`/key show` `/key rotate`\n`/users` `/user hwid:` `/deluser`\n`/sessions` `/bans` `/unban`', inline: true },
      )] });
  }


  if (commandName === 'serverinfo') {
    const g = interaction.guild;
    return interaction.reply({ embeds: [new EmbedBuilder().setTitle(g.name).setColor(CONFIG.color)
      .setThumbnail(g.iconURL({ size: 256 }))
      .addFields(
        { name: 'Members', value: `${g.memberCount}`, inline: true },
        { name: 'Channels', value: `${g.channels.cache.size}`, inline: true },
        { name: 'Roles', value: `${g.roles.cache.size}`, inline: true },
        { name: 'Created', value: `<t:${Math.floor(g.createdTimestamp / 1000)}:R>`, inline: true },
        { name: 'Owner', value: `<@${g.ownerId}>`, inline: true },
        { name: 'Boosts', value: `${g.premiumSubscriptionCount || 0}`, inline: true },
      )] });
  }

  if (commandName === 'userinfo') {
    const user = interaction.options.getUser('user') || interaction.user;
    const member = await interaction.guild.members.fetch(user.id).catch(() => null);
    const e = new EmbedBuilder().setTitle(user.tag).setColor(CONFIG.color)
      .setThumbnail(user.displayAvatarURL({ size: 256 }))
      .addFields({ name: 'ID', value: user.id, inline: true }, { name: 'Created', value: `<t:${Math.floor(user.createdTimestamp / 1000)}:R>`, inline: true });
    if (member) e.addFields({ name: 'Joined', value: `<t:${Math.floor(member.joinedTimestamp / 1000)}:R>`, inline: true },
      { name: 'Roles', value: member.roles.cache.filter(r => r.id !== interaction.guild.id).map(r => `<@&${r.id}>`).join(' ') || 'None' });
    return interaction.reply({ embeds: [e] });
  }

  if (commandName === 'warn') {
    const user = interaction.options.getUser('user');
    const reason = interaction.options.getString('reason');
    if (!warnings[user.id]) warnings[user.id] = [];
    warnings[user.id].push({ by: interaction.user.id, reason, time: Date.now() });
    saveJSON('warnings.json', warnings);
    const count = warnings[user.id].length;
    await interaction.reply({ embeds: [new EmbedBuilder().setTitle('⚠️ Warned').setColor(0xF39C12)
      .setDescription(`<@${user.id}> warned.\n**Reason:** ${reason}\n**Total:** ${count}`)] });
    try { await user.send({ embeds: [new EmbedBuilder().setTitle('⚠️ Warning').setColor(0xF39C12).setDescription(`Warned in **${interaction.guild.name}**\n**Reason:** ${reason}\n**Total:** ${count}`)] }); } catch {}
    if (count >= 3) {
      const m = await interaction.guild.members.fetch(user.id).catch(() => null);
      if (m) { await m.roles.add(CONFIG.roles.muted).catch(() => {}); await interaction.followUp({ content: `<@${user.id}> auto-muted (${count} warns).` }); }
    }
    await logAction(interaction.guild, '⚠️ Warning', `<@${user.id}> by <@${interaction.user.id}> — ${reason} (${count})`, 0xF39C12);
    return;
  }

  if (commandName === 'warnings') {
    const user = interaction.options.getUser('user');
    const w = warnings[user.id] || [];
    if (!w.length) return interaction.reply({ content: `<@${user.id}> has no warnings.`, flags: 64 });
    return interaction.reply({ embeds: [new EmbedBuilder().setTitle(`Warnings — ${user.tag}`).setColor(0xF39C12)
      .setDescription(w.map((x, i) => `**${i+1}.** ${x.reason} — <@${x.by}> <t:${Math.floor(x.time/1000)}:R>`).join('\n'))] });
  }

  if (commandName === 'mute') {
    const user = interaction.options.getUser('user');
    const reason = interaction.options.getString('reason') || 'No reason';
    const m = await interaction.guild.members.fetch(user.id).catch(() => null);
    if (!m) return interaction.reply({ content: '❌ User not found.', flags: 64 });
    await m.roles.add(CONFIG.roles.muted).catch(() => {});
    await interaction.reply({ embeds: [new EmbedBuilder().setTitle('🔇 Muted').setColor(0xE74C3C).setDescription(`<@${user.id}> muted.\n**Reason:** ${reason}`)] });
    await logAction(interaction.guild, '🔇 Mute', `<@${user.id}> by <@${interaction.user.id}> — ${reason}`, 0xE74C3C);
    return;
  }

  if (commandName === 'unmute') {
    const user = interaction.options.getUser('user');
    const m = await interaction.guild.members.fetch(user.id).catch(() => null);
    if (!m) return interaction.reply({ content: '❌ User not found.', flags: 64 });
    await m.roles.remove(CONFIG.roles.muted).catch(() => {});
    await interaction.reply({ embeds: [new EmbedBuilder().setTitle('🔊 Unmuted').setColor(CONFIG.color).setDescription(`<@${user.id}> unmuted.`)] });
    await logAction(interaction.guild, '🔊 Unmute', `<@${user.id}> by <@${interaction.user.id}>`, CONFIG.color);
    return;
  }

  if (commandName === 'kick') {
    const user = interaction.options.getUser('user');
    const reason = interaction.options.getString('reason') || 'No reason';
    const m = await interaction.guild.members.fetch(user.id).catch(() => null);
    if (!m) return interaction.reply({ content: '❌ User not found.', flags: 64 });
    if (!m.kickable) return interaction.reply({ content: '❌ Cannot kick (higher role).', flags: 64 });
    try { await user.send({ embeds: [new EmbedBuilder().setTitle('👢 Kicked').setColor(0xE74C3C).setDescription(`Kicked from **${interaction.guild.name}**\n**Reason:** ${reason}`)] }); } catch {}
    await m.kick(reason);
    await interaction.reply({ embeds: [new EmbedBuilder().setTitle('👢 Kicked').setColor(0xE74C3C).setDescription(`**${user.tag}** kicked.\n**Reason:** ${reason}`)] });
    await logAction(interaction.guild, '👢 Kick', `**${user.tag}** by <@${interaction.user.id}> — ${reason}`, 0xE74C3C);
    return;
  }

  if (commandName === 'ban') {
    const user = interaction.options.getUser('user');
    const reason = interaction.options.getString('reason') || 'No reason';
    const m = await interaction.guild.members.fetch(user.id).catch(() => null);
    if (m && !m.bannable) return interaction.reply({ content: '❌ Cannot ban (higher role).', flags: 64 });
    try { await user.send({ embeds: [new EmbedBuilder().setTitle('🔨 Banned').setColor(0xE74C3C).setDescription(`Banned from **${interaction.guild.name}**\n**Reason:** ${reason}`)] }); } catch {}
    await interaction.guild.members.ban(user.id, { reason });
    await interaction.reply({ embeds: [new EmbedBuilder().setTitle('🔨 Banned').setColor(0xE74C3C).setDescription(`**${user.tag}** banned.\n**Reason:** ${reason}`)] });
    await logAction(interaction.guild, '🔨 Ban', `**${user.tag}** by <@${interaction.user.id}> — ${reason}`, 0xE74C3C);
    return;
  }

  if (commandName === 'purge') {
    const count = interaction.options.getInteger('count');
    try {
      const deleted = await interaction.channel.bulkDelete(count, true);
      await interaction.reply({ content: `✅ Deleted **${deleted.size}** messages.`, flags: 64 });
      await logAction(interaction.guild, '🗑️ Purge', `<@${interaction.user.id}> purged **${deleted.size}** in <#${interaction.channel.id}>`, 0xF39C12);
    } catch (e) { await interaction.reply({ content: `❌ ${e.message}`, flags: 64 }); }
    return;
  }

  if (commandName === 'nuke') {
    const ch = interaction.channel;
    const name = ch.name;
    const position = ch.position;
    const parentId = ch.parentId;
    const topic = ch.topic || '';
    const nsfw = ch.nsfw || false;
    const rateLimit = ch.rateLimitPerUser || 0;
    const type = ch.type;
    const overwrites = ch.permissionOverwrites.cache.map(o => ({
      id: o.id, type: o.type, allow: BigInt(o.allow.bitfield), deny: BigInt(o.deny.bitfield),
    }));
    const oldId = ch.id;
    await interaction.deferReply({ flags: 64 });
    try {
      await ch.delete('Nuked');
      const newCh = await interaction.guild.channels.create({
        name,
        type,
        position,
        topic,
        nsfw,
        rateLimitPerUser: rateLimit,
        parent: parentId,
        permissionOverwrites: overwrites,
        reason: `Nuked by ${interaction.user.tag}`,
      });
      await newCh.send({ embeds: [new EmbedBuilder()
        .setTitle('💥 Channel Nuked')
        .setColor(0xFF0000)
        .setDescription(`This channel was nuked by <@${interaction.user.id}>.`)
        .setTimestamp()] });
      await interaction.editReply({ content: `💥 Nuked <#${oldId}> → <#${newCh.id}>` });
      await logAction(interaction.guild, '💥 Nuke', `<#${oldId}> (\`${name}\`) nuked by <@${interaction.user.id}> → <#${newCh.id}>`, 0xFF0000);
    } catch (e) {
      await interaction.editReply({ content: `❌ Nuke failed: ${e.message}` }).catch(() => {});
    }
    return;
  }

  if (commandName === 'wipe') {
    const count = interaction.options.getInteger('count');
    await interaction.deferReply({ flags: 64 });
    let total = 0;
    try {
      while (total < count) {
        const batch = Math.min(100, count - total);
        const msgs = await interaction.channel.messages.fetch({ limit: batch });
        if (!msgs.size) break;
        const deleted = await interaction.channel.bulkDelete(msgs, true);
        total += deleted.size;
        if (deleted.size < batch) break;
      }
      await interaction.editReply({ content: `✅ Wiped **${total}** messages.` });
      await logAction(interaction.guild, '🧹 Wipe', `<@${interaction.user.id}> wiped **${total}** messages in <#${interaction.channel.id}>`, 0xF39C12);
    } catch (e) {
      await interaction.editReply({ content: `❌ Wipe failed: ${e.message}` }).catch(() => {});
    }
    return;
  }

  if (commandName === 'announce') {
    const title = interaction.options.getString('title');
    const msg = interaction.options.getString('message');
    const ch = interaction.guild.channels.cache.get(CONFIG.channels.announcements);
    if (!ch) return interaction.reply({ content: '❌ Channel not found.', flags: 64 });
    await ch.send({ embeds: [new EmbedBuilder().setTitle(title).setDescription(msg).setColor(CONFIG.color).setTimestamp().setFooter({ text: `by ${interaction.user.tag}` })] });
    await interaction.reply({ content: `✅ Posted in <#${ch.id}>`, flags: 64 });
    return;
  }


  if (commandName === 'lockdown') {
    const ch = interaction.channel;
    const ow = ch.permissionOverwrites.cache.get(interaction.guild.id);
    const locked = ow?.deny.has(PermissionFlagsBits.SendMessages);
    if (locked) {
      await ch.permissionOverwrites.edit(interaction.guild.id, { SendMessages: null });
      await interaction.reply({ embeds: [new EmbedBuilder().setTitle('🔓 Unlocked').setColor(CONFIG.color)] });
    } else {
      await ch.permissionOverwrites.edit(interaction.guild.id, { SendMessages: false });
      await interaction.reply({ embeds: [new EmbedBuilder().setTitle('🔒 Locked').setColor(0xE74C3C).setDescription('Staff only.')] });
    }
    await logAction(interaction.guild, locked ? '🔓 Unlock' : '🔒 Lock', `<#${ch.id}> by <@${interaction.user.id}>`, locked ? CONFIG.color : 0xE74C3C);
    return;
  }

  if (commandName === 'slowmode') {
    const s = interaction.options.getInteger('seconds');
    await interaction.channel.setRateLimitPerUser(s);
    await interaction.reply({ content: s ? `✅ Slowmode: **${s}s**` : '✅ Slowmode off.' });
    return;
  }

  if (commandName === 'setstatus') {
    const game = interaction.options.getString('game');
    const status = interaction.options.getString('status');
    if (!gameStatus[game]) return interaction.reply({ content: '❌ Unknown game.', flags: 64 });
    gameStatus[game].status = status;
    saveJSON('status.json', gameStatus);
    await updateStatusEmbed();
    await interaction.reply({ content: `✅ **${gameStatus[game].label}** → ${STATUS_ICONS[status]} ${status}`, flags: 64 });
    await logAction(interaction.guild, '📡 Status Update', `**${gameStatus[game].label}** set to ${STATUS_ICONS[status]} **${status}** by <@${interaction.user.id}>`, CONFIG.color);
    return;
  }

  // ═══════════ ADMIN PANEL COMMANDS ═══════════

  if (commandName === 'key') {
    await interaction.deferReply({ flags: 64 });
    const sub = interaction.options.getSubcommand();
    try {
      if (sub === 'show') {
        const r = await authAPI('GET', '/admin/key');
        if (r.status !== 200) return interaction.editReply({ content: '❌ Auth server error: ' + r.status });
        return interaction.editReply({ content: `🔑 Current license key: \`${r.json.key}\`` });
      }
      if (sub === 'rotate') {
        const custom = interaction.options.getString('custom');
        const r = await authAPI('POST', '/admin/key', custom ? { key: custom } : {});
        if (r.status !== 200) return interaction.editReply({ content: '❌ Rotation failed: ' + r.status });
        await interaction.editReply({ content: `🔄 Key rotated!\n\n🔑 New key: \`${r.json.key}\`\n\nAll active sessions were invalidated. Update your community.` });
        await logAction(interaction.guild, '🔑 Key Rotated', `New key: \`${r.json.key}\` by <@${interaction.user.id}>`, 0xF39C12);
        return;
      }
    } catch (e) { return interaction.editReply({ content: '❌ ' + e.message }); }
  }

  if (commandName === 'users') {
    await interaction.deferReply({ flags: 64 });
    const page = (interaction.options.getInteger('page') || 1) - 1;
    try { return await interaction.editReply(await renderUsers(page)); }
    catch (e) { return interaction.editReply({ content: '❌ ' + e.message }); }
  }

  if (commandName === 'user') {
    await interaction.deferReply({ flags: 64 });
    const hwid = interaction.options.getString('hwid');
    try {
      const r = await authAPI('GET', '/admin/user?hwid=' + encodeURIComponent(hwid));
      if (r.status === 404) return interaction.editReply({ content: '❌ No user with that HWID.' });
      if (r.status !== 200) return interaction.editReply({ content: '❌ Auth server error: ' + r.status });
      const u = r.json.user;
      const flags = r.json.flags || [];
      const e = new EmbedBuilder()
        .setTitle(`👤 ${u.sn}${flags.length ? ' ⚠️' : ''}`)
        .setColor(flags.length ? 0xE74C3C : CONFIG.color)
        .addFields(
          { name: 'HWID', value: '`' + u.w + '`', inline: true },
          { name: 'Steam', value: `[${u.sn}](https://steamcommunity.com/profiles/${u.s})`, inline: true },
          { name: 'IP', value: '`' + u.ip + '`', inline: true },
          { name: 'Windows User', value: '`' + u.u + '`', inline: true },
          { name: 'Hostname', value: '`' + u.h + '`', inline: true },
          { name: 'Sessions', value: '`' + u.sessions + '`', inline: true },
          { name: 'Downloads', value: '`' + (u.downloads||0) + '`', inline: true },
          { name: 'First Seen', value: `<t:${Math.floor(new Date(u.first_seen).getTime()/1000)}:f>`, inline: true },
          { name: 'Last Seen', value: `<t:${Math.floor(new Date(u.last_seen).getTime()/1000)}:R>`, inline: true },
        );
      if (u.steam_history?.length) e.addFields({ name: '🎮 Steam History', value: u.steam_history.map(s => `[${s.sn||s.s}](https://steamcommunity.com/profiles/${s.s}) (${s.count}x)`).join('\n').slice(0, 1024) });
      if (u.ip_history?.length) e.addFields({ name: '🌐 IP History', value: u.ip_history.slice(-8).map(i => '`' + i.ip + '` (' + i.count + 'x)').join('\n').slice(0, 1024) });
      if (flags.length) e.addFields({ name: '🚨 Flags', value: flags.join('\n') });
      if (u.sys) {
        const sys = Object.entries(u.sys).map(([k,v]) => '**' + k + ':** ' + v).join('\n').slice(0, 1024);
        if (sys) e.addFields({ name: '🖥️ System', value: sys });
      }
      return interaction.editReply({ embeds: [e] });
    } catch (e) { return interaction.editReply({ content: '❌ ' + e.message }); }
  }

  if (commandName === 'deluser') {
    await interaction.deferReply({ flags: 64 });
    const hwid = interaction.options.getString('hwid');
    try {
      const r = await authAPI('DELETE', '/admin/user?hwid=' + encodeURIComponent(hwid), { deleteMsg: true });
      if (r.status === 404) return interaction.editReply({ content: '❌ No user with that HWID.' });
      if (r.status !== 200) return interaction.editReply({ content: '❌ Delete failed: ' + r.status });
      await interaction.editReply({ content: `🗑️ Deleted user \`${hwid}\` and their Discord DB messages.` });
      await logAction(interaction.guild, '🗑️ User Deleted', `HWID \`${hwid}\` by <@${interaction.user.id}>`, 0xE74C3C);
      return;
    } catch (e) { return interaction.editReply({ content: '❌ ' + e.message }); }
  }

  if (commandName === 'sessions') {
    await interaction.deferReply({ flags: 64 });
    try { return await interaction.editReply(await renderSessions()); }
    catch (e) { return interaction.editReply({ content: '❌ ' + e.message }); }
  }

  if (commandName === 'bans') {
    await interaction.deferReply({ flags: 64 });
    try { return await interaction.editReply(await renderBans()); }
    catch (e) { return interaction.editReply({ content: '❌ ' + e.message }); }
  }

  if (commandName === 'panel') {
    await interaction.deferReply({ flags: 64 });
    try {
      const ch = await client.channels.fetch(CONFIG.channels.panel);
      await postPanel(ch);
      return interaction.editReply({ content: '🛠️ Control panel reposted in <#' + CONFIG.channels.panel + '>.' });
    } catch (e) { return interaction.editReply({ content: '❌ ' + e.message }); }
  }
});

// ── Prefix commands ─────────────────────────────────────────────────────
client.on('messageCreate', async (message) => {
  if (message.author.bot || !message.guild || message.guild.id !== CONFIG.guildId) return;
  if (message.member?.roles.cache.has(CONFIG.roles.muted)) { await message.delete().catch(() => {}); return; }
  if (!message.content.startsWith(CONFIG.prefix)) return;
  const args = message.content.slice(1).trim().split(/\s+/);
  const cmd = args.shift().toLowerCase();
  if (cmd === 'help') message.reply('Use `/help` for the full list.');
  else if (cmd === 'ping') message.reply(`🏓 **${Date.now() - message.createdTimestamp}ms**`);
  else if (cmd === 'stats') message.reply({ embeds: [new EmbedBuilder().setTitle('📊 Stats').setColor(CONFIG.color)
    .addFields({ name: 'Uptime', value: `${Math.floor(client.uptime/60000)}m`, inline: true }, { name: 'Members', value: `${message.guild.memberCount}`, inline: true }, { name: 'Keys Issued', value: `${Object.keys(keys.issued).length}`, inline: true })] });
  else if (cmd === 'invite') message.reply('Set your vanity URL in server settings!');
});

// ── Message logging ─────────────────────────────────────────────────────
client.on('messageDelete', async (msg) => {
  if (!msg.guild || msg.guild.id !== CONFIG.guildId || msg.author?.bot) return;
  await logAction(msg.guild, '🗑️ Deleted', `By <@${msg.author?.id}> in <#${msg.channel.id}>\n${msg.content?.slice(0,500)||'*empty*'}`, 0xE74C3C);
});

client.on('messageUpdate', async (old, cur) => {
  if (!old.guild || old.guild.id !== CONFIG.guildId || old.author?.bot || old.content === cur.content) return;
  await logAction(old.guild, '✏️ Edited', `<@${old.author?.id}> in <#${old.channel.id}>\n**Before:** ${old.content?.slice(0,300)}\n**After:** ${cur.content?.slice(0,300)}`, 0xF39C12);
});

client.login(CONFIG.token);
