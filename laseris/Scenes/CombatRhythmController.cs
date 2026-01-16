//using Godot;
//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Threading.Tasks;
//
//public partial class CombatRhythmController : Node
//{
	//[ExportCategory("Refs")]
	//[Export] public NodePath MagePath = "../Mage";
	//[Export] public NodePath EnemyPath = "../Enemy";
	//[Export] public NodePath EnemyAttackerPath = "../Enemy/EnemyRhythmAttacker";
	//[Export] public NodePath RhythmHudPath = "../CanvasLayer/RhythmHud";
//
	//[ExportCategory("Beatmap")]
	//[Export] public string BeatMapJsonPath = "res://Data/BeatMaps/test.json"; // { "beats":[...] }
//
	//[ExportCategory("Music")]
	//[Export] public NodePath MusicPlayerPath = "../MusicPlayer"; // AudioStreamPlayer (ou você pode usar seu MusicService)
	//[Export] public float SongStartOffsetSeconds = 0.0f; // se quiser atrasar/adiantar
//
	//[ExportCategory("Timing")]
	//[Export] public float PreHintLeadSeconds = 0.10f;   // “alguns ms antes”
	//[Export] public float PerfectWindow = 0.045f;
	//[Export] public float GoodWindow = 0.085f;
	//[Export] public float OkWindow = 0.120f;
//
	//[ExportCategory("Turns")]
	//[Export] public float TurnSeconds = 20f; // 20s atacando, 20s defendendo
//
	//[ExportCategory("Elements")]
	//[Export] public ElementCounterTable CounterTable; // .tres
//
	//// ===========================
	//private Mage _mage;
	//private Enemy _enemy;
	//private EnemyRhythmAttacker _enemyAtk;
	//private RhythmHud _hud;
	//private AudioStreamPlayer _music;
//
	//private List<float> _beats = new();
	//private int _beatIndex;
//
	//private float _combatTime;        // tempo “musical” (relógio)
	//private float _turnTime;          // tempo restante no turno atual
	//private bool _enemyTurn = true;   // começa com inimigo atacando
	//private bool _running = false;
//
	//private float _flow01 = 0f;       // 0..1
//
	//// janela atual (beat)
	//private float _currentBeatTime;
	//private bool _prehintFired;
	//private bool _beatFired;
//
	//// inimigo escolhe elemento por beat
	//private ElementType _incomingElement;
//
	//public override async void _Ready()
	//{
		//_mage = GetNodeOrNull<Mage>(MagePath);
		//_enemy = GetNodeOrNull<Enemy>(EnemyPath);
		//_enemyAtk = GetNodeOrNull<EnemyRhythmAttacker>(EnemyAttackerPath);
		//_hud = GetNodeOrNull<RhythmHud>(RhythmHudPath);
		//_music = GetNodeOrNull<AudioStreamPlayer>(MusicPlayerPath);
//
		//if (_mage == null) { GD.PushError("[CombatRhythm] MagePath inválido."); return; }
		//if (_enemy == null) { GD.PushError("[CombatRhythm] EnemyPath inválido."); return; }
		//if (_enemyAtk == null) { GD.PushError("[CombatRhythm] EnemyAttackerPath inválido."); return; }
		//if (_hud == null) GD.PushWarning("[CombatRhythm] RhythmHudPath inválido (sem HUD).");
		//if (_music == null) GD.PushWarning("[CombatRhythm] MusicPlayerPath inválido (sem música).");
		//if (CounterTable == null) GD.PushWarning("[CombatRhythm] CounterTable null. Counters vão virar “qualquer tecla”.");
		//
		//LoadBeatMapFromJson(BeatMapJsonPath);
//
		//await DoCountdown();
//
		//StartCombat();
	//}
//
	//public override void _Process(double delta)
	//{
		//if (!_running) return;
		//float dt = (float)delta;
//
		//// relógio principal: se tiver música, você pode substituir isso por playback position
		//_combatTime += dt;
//
		//_turnTime -= dt;
		//if (_turnTime <= 0f)
			//SwitchTurn();
//
		//if (_beatIndex >= _beats.Count) return;
//
		//_currentBeatTime = _beats[_beatIndex];
		//float preTime = _currentBeatTime - PreHintLeadSeconds;
//
		//// prehint
		//if (!_prehintFired && _combatTime + SongStartOffsetSeconds >= preTime)
		//{
			//_prehintFired = true;
			//_hud?.PulsePreHint();
			//
			//if (_enemyTurn)
			//{
				//_incomingElement = ChooseEnemyElementForBeat(_beatIndex);
				//_enemyAtk.PrepareAttack(_incomingElement, _beatIndex);
			//}
		//}
//
		//// beat
		//if (!_beatFired && _combatTime + SongStartOffsetSeconds >= _currentBeatTime)
		//{
			//_beatFired = true;
			//_hud?.PulseBeatHint();
//
			//if (_enemyTurn)
			//{
				//// se player não apertou nada até aqui, é miss (vai tomar)
				//bool blocked = false;
				//_enemyAtk.DoAttack(_incomingElement, _mage, blocked, _flow01);
				//RegisterGrade(TimingGrade.Miss, "MISS");
			//}
//
			//// avança pra próxima batida
			//AdvanceBeat();
		//}
	//}
//
	//public override void _UnhandledInput(InputEvent e)
	//{
		//if (!_running) return;
//
		//if (e is not InputEventKey) return;
//
		//// pega elemento apertado
		//if (!TryReadElementInput(out var element)) return;
//
		//// timing check: bate no beat mais próximo (o atual)
		//float tNow = _combatTime + SongStartOffsetSeconds;
//
		//// se ainda não chegou no beat atual, podemos usar o beat atual mesmo
		//float beatT = _currentBeatTime;
		//float offset = tNow - beatT; // negativo = adiantado
		//float abs = Mathf.Abs(offset);
//
		//var grade = Evaluate(abs);
//
		//if (_enemyTurn)
		//{
			//// DEFESA: tem que apertar o counter do elemento que está vindo
			//bool okElement = true;
			//if (CounterTable != null)
			//{
				//var required = CounterTable.GetCounter(_incomingElement);
				//okElement = (element == required);
			//}
//
			//if (grade == TimingGrade.Miss || !okElement)
			//{
				//_enemyAtk.DoAttack(_incomingElement, _mage, blocked: false, _flow01);
				//RegisterGrade(TimingGrade.Miss, !okElement ? "WRONG" : "MISS");
				//PunishTurnTime();
				//return;
			//}
//
			//// bloqueou: projétil gruda no escudo
			//_enemyAtk.DoAttack(_incomingElement, _mage, blocked: true, _flow01);
			//RegisterGrade(grade, GradeText(grade));
			//RewardFlow(grade);
//
			//// consumiu a batida (evita “double”)
			//AdvanceBeat();
			//return;
		//}
//
		//// TURNO DO PLAYER (ATAQUE)
		//if (grade == TimingGrade.Miss)
		//{
			//RegisterGrade(TimingGrade.Miss, "MISS");
			//PunishTurnTime();
			//DropFlow();
			//return;
		//}
//
		//// aqui você decide a regra do escudo:
		//// - inimigo tem shield element (ex: 1 ativo)
		//// - player deve apertar o counter dele pra dar “super hit”
		//// vou fazer: counter = dano maior / errado = dano menor (mas ainda bate)
		//var shieldElem = _enemy.ShieldPrimaryOrFallback();
		//bool isCounter = (CounterTable != null) ? (element == CounterTable.GetCounter(shieldElem)) : true;
//
		//DoPlayerAttack(element, isCounter, grade);
//
		//RegisterGrade(grade, (isCounter ? "HIT " : "WEAK ") + GradeText(grade));
		//RewardFlow(grade);
//
		//AdvanceBeat();
	//}
//
	//// ===========================
	//private async Task DoCountdown()
	//{
		//_hud?.SetCountdown("3");
		//await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);
		//_hud?.SetCountdown("2");
		//await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);
		//_hud?.SetCountdown("1");
		//await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);
		//_hud?.SetCountdown("READY");
		//await ToSignal(GetTree().CreateTimer(0.6f), SceneTreeTimer.SignalName.Timeout);
		//_hud?.HideCountdown();
	//}
//
	//private void StartCombat()
	//{
		//_running = true;
		//_combatTime = 0f;
		//_turnTime = TurnSeconds;
		//_enemyTurn = true;
//
		//_flow01 = 0f;
		//_hud?.SetFlow(_flow01);
//
		//_beatIndex = 0;
		//ResetBeatFlags();
//
		//if (_music != null)
			//_music.Play();
	//}
//
	//private void SwitchTurn()
	//{
		//_enemyTurn = !_enemyTurn;
		//_turnTime = TurnSeconds;
//
		//// você pode resetar flow ao trocar turno, ou não:
		//// _flow01 = Mathf.Max(0f, _flow01 - 0.15f);
//
		//if (_enemyTurn)
		//{
			//// inimigo “troca stance” no começo
			//_incomingElement = ChooseEnemyElementForBeat(_beatIndex);
		//}
	//}
//
	//private void PunishTurnTime()
	//{
		//// errar elemento ou timing reduz tempo do turno
		//_turnTime = Mathf.Max(0f, _turnTime - 1.25f);
	//}
//
	//private void DropFlow()
	//{
		//_flow01 = Mathf.Clamp(_flow01 - 0.12f, 0f, 1f);
		//_hud?.SetFlow(_flow01);
	//}
//
	//private void RewardFlow(TimingGrade g)
	//{
		//float add = g switch
		//{
			//TimingGrade.Perfect => 0.10f,
			//TimingGrade.Good => 0.06f,
			//TimingGrade.Ok => 0.03f,
			//_ => 0f
		//};
		//_flow01 = Mathf.Clamp(_flow01 + add, 0f, 1f);
		//_hud?.SetFlow(_flow01);
	//}
//
	//private void RegisterGrade(TimingGrade g, string text)
	//{
		//_hud?.SetGrade(text);
		//if (g == TimingGrade.Miss) DropFlow();
	//}
//
	//private void AdvanceBeat()
	//{
		//_beatIndex++;
		//ResetBeatFlags();
	//}
//
	//private void ResetBeatFlags()
	//{
		//_prehintFired = false;
		//_beatFired = false;
//
		//if (_beatIndex < _beats.Count)
			//_currentBeatTime = _beats[_beatIndex];
	//}
//
	//private TimingGrade Evaluate(float absOffset)
	//{
		//if (absOffset <= PerfectWindow) return TimingGrade.Perfect;
		//if (absOffset <= GoodWindow) return TimingGrade.Good;
		//if (absOffset <= OkWindow) return TimingGrade.Ok;
		//return TimingGrade.Miss;
	//}
//
	//private string GradeText(TimingGrade g) => g switch
	//{
		//TimingGrade.Perfect => "PERFECT",
		//TimingGrade.Good => "GOOD",
		//TimingGrade.Ok => "OK",
		//_ => "MISS"
	//};
//
	//private ElementType ChooseEnemyElementForBeat(int beatIndex)
	//{
		//// simples e “vivo”: alterna elementos baseado no beatIndex
		//var pool = new[]
		//{
			//ElementType.Fire, ElementType.Ice, ElementType.Lightning, ElementType.Earth,
			//ElementType.Air, ElementType.Poison, ElementType.Light, ElementType.Shadow
		//};
		//return pool[beatIndex % pool.Length];
	//}
//
	//private void DoPlayerAttack(ElementType element, bool isCounter, TimingGrade grade)
	//{
		//// aqui você pluga seu pipeline real (SFX/VFX/Enemy.TakeSpellHit).
		//// Por enquanto: dano simples pra validar mecânica.
		//int baseDmg = isCounter ? 30 : 10;
//
		//float timingMult = grade switch
		//{
			//TimingGrade.Perfect => 1.35f,
			//TimingGrade.Good => 1.10f,
			//TimingGrade.Ok => 0.95f,
			//_ => 0.75f
		//};
//
		//float flowMult = Mathf.Lerp(0.85f, 1.5f, _flow01);
//
		//int dmg = Mathf.RoundToInt(baseDmg * timingMult * flowMult);
//
		//_enemy.TakeDamage(dmg);
	//}
//
	//private bool TryReadElementInput(out ElementType element)
	//{
		//element = ElementType.Fire;
//
		//if (Input.IsActionJustPressed("elem_fire")) { element = ElementType.Fire; return true; }
		//if (Input.IsActionJustPressed("elem_ice")) { element = ElementType.Ice; return true; }
		//if (Input.IsActionJustPressed("elem_lightning")) { element = ElementType.Lightning; return true; }
		//if (Input.IsActionJustPressed("elem_poison")) { element = ElementType.Poison; return true; }
		//if (Input.IsActionJustPressed("elem_earth")) { element = ElementType.Earth; return true; }
		//if (Input.IsActionJustPressed("elem_air")) { element = ElementType.Air; return true; }
		//if (Input.IsActionJustPressed("elem_light")) { element = ElementType.Light; return true; }
		//if (Input.IsActionJustPressed("elem_shadow")) { element = ElementType.Shadow; return true; }
//
		//return false;
	//}
//
	//private void LoadBeatMapFromJson(string path)
	//{
		//_beats.Clear();
//
		//if (!ResourceLoader.Exists(path))
		//{
			//GD.PushWarning($"[CombatRhythm] BeatMapJsonPath não existe: {path}");
			//return;
		//}
//
		//var txt = FileAccess.GetFileAsString(path);
		//if (string.IsNullOrWhiteSpace(txt))
		//{
			//GD.PushWarning("[CombatRhythm] BeatMap JSON vazio.");
			//return;
		//}
//
		//// JSON simples: { "beats":[1.36, 1.5, ...] }
		//var json = Json.ParseString(txt).AsGodotDictionary();
		//if (json == null || !json.ContainsKey("beats"))
		//{
			//GD.PushWarning("[CombatRhythm] JSON sem key 'beats'.");
			//return;
		//}
//
		//var arr = json["beats"].AsGodotArray();
		//foreach (var v in arr)
		//{
			//if (v.VariantType == Variant.Type.Float || v.VariantType == Variant.Type.Int)
				//_beats.Add((float)v);
			//else if (v.VariantType == Variant.Type.String)
			//{
				//if (float.TryParse((string)v, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
					//_beats.Add(f);
			//}
		//}
//
		//_beats.Sort();
//
		//GD.Print($"[CombatRhythm] Loaded beats={_beats.Count} from {path}");
	//}
//
	//private enum TimingGrade { Perfect, Good, Ok, Miss }
//}
