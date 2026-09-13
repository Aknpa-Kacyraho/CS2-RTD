using System.Text.Json.Serialization;
using RollTheDice.Configs;

namespace RollTheDice;

public class DicesConfig
{
	[JsonPropertyName("rarity")]
	public RarityConfig Rarity { get; set; } = new RarityConfig();

	[JsonPropertyName("high_gravity")]
	public HighGravityConfig HighGravity { get; set; } = new HighGravityConfig();

	[JsonPropertyName("kinship")]
	public KinshipConfig Kinship { get; set; } = new KinshipConfig();

	[JsonPropertyName("increase_speed")]
	public IncreaseSpeedConfig IncreaseSpeed { get; set; } = new IncreaseSpeedConfig();

	[JsonPropertyName("no_recoil")]
	public NoRecoilConfig NoRecoil { get; set; } = new NoRecoilConfig();

	[JsonPropertyName("respawn")]
	public RespawnConfig Respawn { get; set; } = new RespawnConfig();

	[JsonPropertyName("no_explosives")]
	public NoExplosivesConfig NoExplosives { get; set; } = new NoExplosivesConfig();

	[JsonPropertyName("vampire")]
	public VampireConfig Vampire { get; set; } = new VampireConfig();

	[JsonPropertyName("fog_of_war")]
	public FogOfWarConfig FogOfWar { get; set; } = new FogOfWarConfig();

	[JsonPropertyName("play_as_chicken")]
	public PlayAsChickenConfig PlayAsChicken { get; set; } = new PlayAsChickenConfig();

	[JsonPropertyName("unlimited_ammo")]
	public InfiniteAmmoConfig InfiniteAmmo { get; set; } = new InfiniteAmmoConfig();

	[JsonPropertyName("damage_multiplier")]
	public DamageMultiplierConfig DamageMultiplier { get; set; } = new DamageMultiplierConfig();

	[JsonPropertyName("longer_flashes")]
	public LongerFlashesConfig LongerFlashes { get; set; } = new LongerFlashesConfig();

	[JsonPropertyName("reset_on_reload")]
	public ResetOnReloadConfig ResetOnReload { get; set; } = new ResetOnReloadConfig();

	[JsonPropertyName("cutter")]
	public CutterConfig Cutter { get; set; } = new CutterConfig();

	[JsonPropertyName("return_to_sender")]
	public ReturnToSenderConfig ReturnToSender { get; set; } = new ReturnToSenderConfig();

	[JsonPropertyName("jammer")]
	public JammerConfig Jammer { get; set; } = new JammerConfig();

	[JsonPropertyName("deaf")]
	public DeafConfig Deaf { get; set; } = new DeafConfig();

	[JsonPropertyName("berserker")]
	public BerserkerConfig Berserker { get; set; } = new BerserkerConfig();

	[JsonPropertyName("guardian_angel")]
	public GuardianAngelConfig GuardianAngel { get; set; } = new GuardianAngelConfig();

	[JsonPropertyName("martyrdom")]
	public MartyrdomConfig Martyrdom { get; set; } = new MartyrdomConfig();

	[JsonPropertyName("smoke_vision")]
	public SmokeVisionConfig SmokeVision { get; set; } = new SmokeVisionConfig();

	[JsonPropertyName("evasion")]
	public EvasionConfig Evasion { get; set; } = new EvasionConfig();

	[JsonPropertyName("ice_beam")]
	public IceBeamConfig IceBeam { get; set; } = new IceBeamConfig();

	[JsonPropertyName("ice_dragon")]
	public IceDragonConfig IceDragon { get; set; } = new IceDragonConfig();

	[JsonPropertyName("speed_on_kill")]
	public SpeedOnKillConfig SpeedOnKill { get; set; } = new SpeedOnKillConfig();

	[JsonPropertyName("poison_blade")]
	public PoisonBladeConfig PoisonBlade { get; set; } = new PoisonBladeConfig();

	[JsonPropertyName("shield")]
	public ShieldConfig Shield { get; set; } = new ShieldConfig();

	[JsonPropertyName("thorns")]
	public ThornsConfig Thorns { get; set; } = new ThornsConfig();

	[JsonPropertyName("adrenaline")]
	public AdrenalineConfig Adrenaline { get; set; } = new AdrenalineConfig();

	[JsonPropertyName("bounty")]
	public BountyConfig Bounty { get; set; } = new BountyConfig();

	[JsonPropertyName("pickpocket")]
	public PickpocketConfig Pickpocket { get; set; } = new PickpocketConfig();

	[JsonPropertyName("lucky")]
	public LuckyConfig Lucky { get; set; } = new LuckyConfig();

	[JsonPropertyName("payback")]
	public PaybackConfig Payback { get; set; } = new PaybackConfig();

	[JsonPropertyName("phoenix")]
	public PhoenixConfig Phoenix { get; set; } = new PhoenixConfig();

	[JsonPropertyName("regeneration")]
	public RegenerationConfig Regeneration { get; set; } = new RegenerationConfig();

	[JsonPropertyName("fireball")]
	public FireballConfig Fireball { get; set; } = new FireballConfig();

	[JsonPropertyName("fire_dragon")]
	public FireDragonConfig FireDragon { get; set; } = new FireDragonConfig();

	[JsonPropertyName("smoke_bomb")]
	public SmokeBombConfig SmokeBomb { get; set; } = new SmokeBombConfig();

	[JsonPropertyName("divine_resurrection")]
	public DivineResurrectionConfig DivineResurrection { get; set; } = new DivineResurrectionConfig();

	[JsonPropertyName("imposter_syndrome")]
	public ImposterSyndromeConfig ImposterSyndrome { get; set; } = new ImposterSyndromeConfig();

	[JsonPropertyName("cupid")]
	public CupidConfig Cupid { get; set; } = new CupidConfig();

	[JsonPropertyName("royal_barrier")]
	public RoyalBarrierConfig RoyalBarrier { get; set; } = new RoyalBarrierConfig();

	[JsonPropertyName("pistol_master")]
	public PistolMasterConfig PistolMaster { get; set; } = new PistolMasterConfig();

	[JsonPropertyName("prophet")]
	public ProphetConfig Prophet { get; set; } = new ProphetConfig();

	[JsonPropertyName("weapon_roulette")]
	public WeaponRouletteConfig WeaponRoulette { get; set; } = new WeaponRouletteConfig();

	[JsonPropertyName("sword_saint")]
	public SwordSaintConfig SwordSaint { get; set; } = new SwordSaintConfig();

	[JsonPropertyName("glutton")]
	public GluttonConfig Glutton { get; set; } = new GluttonConfig();

	[JsonPropertyName("c4_expert")]
	public C4ExpertConfig C4Expert { get; set; } = new C4ExpertConfig();

	[JsonPropertyName("grenade_king")]
	public GrenadeKingConfig GrenadeKing { get; set; } = new GrenadeKingConfig();

	[JsonPropertyName("roulette_gambler")]
	public RouletteGamblerConfig RouletteGambler { get; set; } = new RouletteGamblerConfig();

	[JsonPropertyName("izayoi")]
	public IzayoiConfig Izayoi { get; set; } = new IzayoiConfig();

	[JsonPropertyName("toxic_smoke")]
	public ToxicSmokeConfig ToxicSmoke { get; set; } = new ToxicSmokeConfig();

	[JsonPropertyName("capitalist")]
	public CapitalistConfig Capitalist { get; set; } = new CapitalistConfig();

	[JsonPropertyName("knight")]
	public KnightConfig Knight { get; set; } = new KnightConfig();

	[JsonPropertyName("god")]
	public GodConfig God { get; set; } = new GodConfig();

	[JsonPropertyName("goddess")]
	public GoddessConfig Goddess { get; set; } = new GoddessConfig();

	[JsonPropertyName("hanged_man")]
	public HangedManConfig HangedMan { get; set; } = new HangedManConfig();

	[JsonPropertyName("radar_station")]
	public RadarStationConfig RadarStation { get; set; } = new RadarStationConfig();

	[JsonPropertyName("gun_healer")]
	public GunHealerConfig GunHealer { get; set; } = new GunHealerConfig();

	[JsonPropertyName("mosquito")]
	public MosquitoConfig Mosquito { get; set; } = new MosquitoConfig();

	[JsonPropertyName("giant")]
	public GiantConfig Giant { get; set; } = new GiantConfig();

	[JsonPropertyName("fire_lord")]
	public FireLordConfig FireLord { get; set; } = new FireLordConfig();

	[JsonPropertyName("forsaken")]
	public ForsakenConfig Forsaken { get; set; } = new ForsakenConfig();

	[JsonPropertyName("tactician")]
	public TacticianConfig Tactician { get; set; } = new TacticianConfig();

	[JsonPropertyName("jester")]
	public JesterConfig Jester { get; set; } = new JesterConfig();

	[JsonPropertyName("deagle_king")]
	public DeagleKingConfig DeagleKing { get; set; } = new DeagleKingConfig();

	[JsonPropertyName("priest")]
	public PriestConfig Priest { get; set; } = new PriestConfig();

	[JsonPropertyName("pope")]
	public PopeConfig Pope { get; set; } = new PopeConfig();

	[JsonPropertyName("emperor")]
	public EmperorConfig Emperor { get; set; } = new EmperorConfig();

	[JsonPropertyName("world")]
	public WorldConfig World { get; set; } = new WorldConfig();

	[JsonPropertyName("hermit")]
	public HermitConfig Hermit { get; set; } = new HermitConfig();

	[JsonPropertyName("wheel_of_fate")]
	public WheelOfFateConfig WheelOfFate { get; set; } = new WheelOfFateConfig();

	[JsonPropertyName("empress")]
	public EmpressConfig Empress { get; set; } = new EmpressConfig();

	[JsonPropertyName("fool")]
	public FoolConfig Fool { get; set; } = new FoolConfig();

	[JsonPropertyName("frostmourne")]
	public FrostmourneConfig Frostmourne { get; set; } = new FrostmourneConfig();

	[JsonPropertyName("afterimage")]
	public AfterimageConfig Afterimage { get; set; } = new AfterimageConfig();

	[JsonPropertyName("thunder_chain")]
	public ThunderChainConfig ThunderChain { get; set; } = new ThunderChainConfig();

	[JsonPropertyName("beyond_heaven")]
	public BeyondHeavenConfig BeyondHeaven { get; set; } = new BeyondHeavenConfig();

	[JsonPropertyName("laser_cage")]
	public LaserCageConfig LaserCage { get; set; } = new LaserCageConfig();

	[JsonPropertyName("divine_punishment")]
	public DivinePunishmentConfig DivinePunishment { get; set; } = new DivinePunishmentConfig();

	[JsonPropertyName("bone_maggot")]
	public BoneMaggotConfig BoneMaggot { get; set; } = new BoneMaggotConfig();

	[JsonPropertyName("soul_eater")]
	public SoulEaterConfig SoulEater { get; set; } = new SoulEaterConfig();

	[JsonPropertyName("repulsion_field")]
	public RepulsionFieldConfig RepulsionField { get; set; } = new RepulsionFieldConfig();

	[JsonPropertyName("radar_jammer")]
	public RadarJammerConfig RadarJammer { get; set; } = new RadarJammerConfig();

	[JsonPropertyName("decoy_dummy")]
	public DecoyDummyConfig DecoyDummy { get; set; } = new DecoyDummyConfig();

	[JsonPropertyName("jump_heal")]
	public JumpHealConfig JumpHeal { get; set; } = new JumpHealConfig();

	[JsonPropertyName("drone")]
	public DroneConfig Drone { get; set; } = new DroneConfig();

	[JsonPropertyName("dusk_dawn")]
	public DuskDawnConfig DuskDawn { get; set; } = new DuskDawnConfig();

	[JsonPropertyName("shadow_warrior")]
	public ShadowWarriorConfig ShadowWarrior { get; set; } = new ShadowWarriorConfig();

	[JsonPropertyName("titanfall")]
	public TitanfallConfig Titanfall { get; set; } = new TitanfallConfig();

	[JsonPropertyName("necromancer")]
	public NecromancerConfig Necromancer { get; set; } = new NecromancerConfig();

	[JsonPropertyName("disarm")]
	public DisarmConfig Disarm { get; set; } = new DisarmConfig();

	[JsonPropertyName("wasd_chaos")]
	public WASDChaosConfig WASDChaos { get; set; } = new WASDChaosConfig();

	[JsonPropertyName("gargoyle")]
	public GargoyleConfig Gargoyle { get; set; } = new GargoyleConfig();

	[JsonPropertyName("mimic")]
	public MimicConfig Mimic { get; set; } = new MimicConfig();

	[JsonPropertyName("nuke_leak")]
	public NukeLeakConfig NukeLeak { get; set; } = new NukeLeakConfig();

	[JsonPropertyName("lottery")]
	public LotteryConfig Lottery { get; set; } = new LotteryConfig();

	[JsonPropertyName("miser")]
	public MiserConfig Miser { get; set; } = new MiserConfig();

	[JsonPropertyName("redemption")]
	public RedemptionConfig Redemption { get; set; } = new RedemptionConfig();

	[JsonPropertyName("traitor")]
	public TraitorConfig Traitor { get; set; } = new TraitorConfig();

	[JsonPropertyName("eclipse")]
	public EclipseConfig Eclipse { get; set; } = new EclipseConfig();

	[JsonPropertyName("awakener")]
	public AwakenerConfig Awakener { get; set; } = new AwakenerConfig();

	[JsonPropertyName("paladin")]
	public PaladinConfig Paladin { get; set; } = new PaladinConfig();

	[JsonPropertyName("trickster")]
	public TricksterConfig Trickster { get; set; } = new TricksterConfig();

	[JsonPropertyName("dragonborn")]
	public DragonbornConfig Dragonborn { get; set; } = new DragonbornConfig();

	[JsonPropertyName("dragon_soul")]
	public DragonSoulConfig DragonSoul { get; set; } = new DragonSoulConfig();

	[JsonPropertyName("sacrifice")]
	public SacrificeConfig Sacrifice { get; set; } = new SacrificeConfig();

	[JsonPropertyName("plague")]
	public PlagueConfig Plague { get; set; } = new PlagueConfig();

	[JsonPropertyName("infinite_proliferation")]
	public InfiniteProliferationConfig InfiniteProliferation { get; set; } = new InfiniteProliferationConfig();

	[JsonPropertyName("sniper_elite")]
	public SniperEliteConfig SniperElite { get; set; } = new SniperEliteConfig();

	[JsonPropertyName("nirvana")]
	public NirvanaConfig Nirvana { get; set; } = new NirvanaConfig();

	[JsonPropertyName("skyline")]
	public SkylineConfig Skyline { get; set; } = new SkylineConfig();

	[JsonPropertyName("black_hole")]
	public BlackHoleConfig BlackHole { get; set; } = new BlackHoleConfig();

	[JsonPropertyName("white_hole")]
	public WhiteHoleConfig WhiteHole { get; set; } = new WhiteHoleConfig();

	[JsonPropertyName("karma")]
	public KarmaConfig Karma { get; set; } = new KarmaConfig();

	[JsonPropertyName("reincarnation")]
	public ReincarnationConfig Reincarnation { get; set; } = new ReincarnationConfig();

	[JsonPropertyName("reverse_causality")]
	public ReverseCausalityConfig ReverseCausality { get; set; } = new ReverseCausalityConfig();

	[JsonPropertyName("evolution")]
	public EvolutionConfig Evolution { get; set; } = new EvolutionConfig();

	[JsonPropertyName("gravity_well")]
	public GravityWellConfig GravityWell { get; set; } = new GravityWellConfig();

	[JsonPropertyName("bank")]
	public BankConfig Bank { get; set; } = new BankConfig();

	[JsonPropertyName("sacrifice_self")]
	public SacrificeSelfConfig SacrificeSelf { get; set; } = new SacrificeSelfConfig();

	[JsonPropertyName("synced")]
	public SyncedConfig Synced { get; set; } = new SyncedConfig();

	[JsonPropertyName("dead_hand")]
	public DeadHandConfig DeadHand { get; set; } = new DeadHandConfig();

	[JsonPropertyName("hot_potato")]
	public HotPotatoConfig HotPotato { get; set; } = new HotPotatoConfig();

	[JsonPropertyName("frontline_beast")]
	public FrontlineBeastConfig FrontlineBeast { get; set; } = new FrontlineBeastConfig();

	[JsonPropertyName("four_horsemen")]
	public FourHorsemenConfig FourHorsemen { get; set; } = new FourHorsemenConfig();

	[JsonPropertyName("chaos_storm")]
	public ChaosStormConfig ChaosStorm { get; set; } = new ChaosStormConfig();

	[JsonPropertyName("info_hole")]
	public InfoHoleConfig InfoHole { get; set; } = new InfoHoleConfig();

	[JsonPropertyName("countdown")]
	public CountdownConfig Countdown { get; set; } = new CountdownConfig();

	[JsonPropertyName("cthulhu")]
	public CthulhuConfig Cthulhu { get; set; } = new CthulhuConfig();

	[JsonPropertyName("loan_shark")]
	public LoanSharkConfig LoanShark { get; set; } = new LoanSharkConfig();

	[JsonPropertyName("magnetic_pulse")]
	public MagneticPulseConfig MagneticPulse { get; set; } = new MagneticPulseConfig();

	[JsonPropertyName("fate")]
	public FateConfig Fate { get; set; } = new FateConfig();

	[JsonPropertyName("gun_god")]
	public GunGodConfig GunGod { get; set; } = new GunGodConfig();

	[JsonPropertyName("overheat")]
	public OverheatConfig Overheat { get; set; } = new OverheatConfig();

	[JsonPropertyName("sly_fox")]
	public SlyFoxConfig SlyFox { get; set; } = new SlyFoxConfig();

	[JsonPropertyName("singularity")]
	public SingularityConfig Singularity { get; set; } = new SingularityConfig();

	[JsonPropertyName("void")]
	public VoidConfig Void { get; set; } = new VoidConfig();

	[JsonPropertyName("parasite")]
	public ParasiteConfig Parasite { get; set; } = new ParasiteConfig();

	[JsonPropertyName("death_knight")]
	public DeathKnightConfig DeathKnight { get; set; } = new DeathKnightConfig();

	[JsonPropertyName("death_knight_complete")]
	public DeathKnightCompleteConfig DeathKnightComplete { get; set; } = new DeathKnightCompleteConfig();

	[JsonPropertyName("bugle")]
	public BugleConfig Bugle { get; set; } = new BugleConfig();

	[JsonPropertyName("prayer")]
	public PrayerConfig Prayer { get; set; } = new PrayerConfig();

	[JsonPropertyName("gaia")]
	public GaiaConfig Gaia { get; set; } = new GaiaConfig();

	[JsonPropertyName("corona")]
	public CoronaConfig Corona { get; set; } = new CoronaConfig();

	[JsonPropertyName("ragnarok")]
	public RagnarokConfig Ragnarok { get; set; } = new RagnarokConfig();

	[JsonPropertyName("twilight")]
	public TwilightConfig Twilight { get; set; } = new TwilightConfig();

	[JsonPropertyName("heaven")]
	public HeavenConfig Heaven { get; set; } = new HeavenConfig();

	[JsonPropertyName("nightglow")]
	public NightglowConfig Nightglow { get; set; } = new NightglowConfig();

	[JsonPropertyName("taotie")]
	public TaotieConfig Taotie { get; set; } = new TaotieConfig();

	[JsonPropertyName("fibonacci")]
	public FibonacciConfig Fibonacci { get; set; } = new FibonacciConfig();

	[JsonPropertyName("fourty_two")]
	public FourtyTwoConfig FourtyTwo { get; set; } = new FourtyTwoConfig();

	[JsonPropertyName("satellite")]
	public SatelliteConfig Satellite { get; set; } = new SatelliteConfig();

	[JsonPropertyName("amber")]
	public AmberConfig Amber { get; set; } = new AmberConfig();

	[JsonPropertyName("wolf_king")]
	public WolfKingConfig WolfKing { get; set; } = new WolfKingConfig();

	[JsonPropertyName("wolf")]
	public WolfConfig Wolf { get; set; } = new WolfConfig();

	[JsonPropertyName("combo")]
	public ComboConfig Combo { get; set; } = new ComboConfig();

	[JsonPropertyName("pain_converter")]
	public PainConverterConfig PainConverter { get; set; } = new PainConverterConfig();

	[JsonPropertyName("anatomist")]
	public AnatomistConfig Anatomist { get; set; } = new AnatomistConfig();

	[JsonPropertyName("iron_head")]
	public IronHeadConfig IronHead { get; set; } = new IronHeadConfig();

	[JsonPropertyName("guillotine")]
	public GuillotineConfig Guillotine { get; set; } = new GuillotineConfig();

	[JsonPropertyName("crouch")]
	public CrouchConfig Crouch { get; set; } = new CrouchConfig();

	[JsonPropertyName("last_stand")]
	public LastStandConfig LastStand { get; set; } = new LastStandConfig();

	[JsonPropertyName("reload_gap")]
	public ReloadGapConfig ReloadGap { get; set; } = new ReloadGapConfig();

	[JsonPropertyName("rally")]
	public RallyConfig Rally { get; set; } = new RallyConfig();

	[JsonPropertyName("echo")]
	public EchoConfig Echo { get; set; } = new EchoConfig();

	[JsonPropertyName("curse")]
	public CurseConfig Curse { get; set; } = new CurseConfig();

	[JsonPropertyName("yagorou")]
	public YagorouConfig Yagorou { get; set; } = new YagorouConfig();

	[JsonPropertyName("teneril")]
	public TenerilConfig Teneril { get; set; } = new TenerilConfig();
}
