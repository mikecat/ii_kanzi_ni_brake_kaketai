using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using TrainCrew;

class IWannaUseBrakeWell: Form
{
	public static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.Run(new IWannaUseBrakeWell());
	}

	private const int fontSize = 16, gridSize = 22;

	private static Size GetSizeOnGrid(float width, float height)
	{
		return new Size((int)(gridSize * width), (int)(gridSize * height));
	}

	private static Point GetPointOnGrid(float x, float y)
	{
		return new Point((int)(gridSize * x), (int)(gridSize * y));
	}

	private static T CreateControl<T>(Control parent, float x, float y, float width, float height)
	where T: Control, new()
	{
		T control = new T();
		control.Location = GetPointOnGrid(x, y);
		control.Size = GetSizeOnGrid(width, height);
		if (parent != null) parent.Controls.Add(control);
		return control;
	}

	private GroupBox brakeInfoGroupBox;
	private Label brakeInfoAccelTitleLabel, brakeInfoStopDistTitleLabel, brakeInfoLimitDistTitleLabel;
	private Panel[] brakeInfoPanels = new Panel[9];
	private Label[] brakeInfoNameLabels = new Label[9];
	private Label[] brakeInfoAccelLabel = new Label[9];
	private Label[] brakeInfoStopDistLabel = new Label[9];
	private Label[] brakeInfoLimitDistLabel = new Label[9];

	private GroupBox trainInfoGroupBox;
	private Label currentSpeedTitleLabel, currentSpeedLabel;
	private Label currentAccelTitleLabel, currentAccelLabel;
	private Label currentDistanceTitleLabel, currentDistanceLabel;
	private Label currentStopPredictTitleLabel, currentStopPredictLabel;
	private Label currentLimitDistanceTitleLabel, currentLimitDistanceLabel;
	private Label currentBelowLimitPredictTitleLabel, currentBelowLimitPredictLabel;

	private GroupBox configGroupBox;
	private CheckBox useAutoBrakeCheckBox;
	private Label accelSampleIntervalTitleLabel, accelSampleIntervalUnitLabel;
	private NumericUpDown accelSampleIntervalNumericUpDown;
	private Label noConsecutiveOperationTitleLabel, noConsecutiveOperationUnitLabel;
	private NumericUpDown noConsecutiveOperationNumericUpDown;
	private Label speedLimitMarginTitleLabel, speedLimitMarginUnitLabel;
	private NumericUpDown speedLimitMarginNumericUpDown;

	private Timer timer = null;
	private Stopwatch stopwatch;
	private bool trainCrewValid = false;
	private int prevPower = -99, prevBrake = -99;
	private long prevPowerChangedTime = 0, prevBrakeChangedTime = 0;
	private bool prevGaming = false, prevPaused = false;
	private long prevGameStartTime = 0, prevPauseEndTime = 0;

	private struct SpeedInfo
	{
		public readonly long Time;
		public readonly float Speed;

		public SpeedInfo(long time, float speed)
		{
			Time = time;
			Speed = speed;
		}
	}
	private readonly Queue<SpeedInfo> speedInfoQueue = new Queue<SpeedInfo>();
	private SpeedInfo? accelSample = null;
	private float?[] accelByBrakes = new float?[9];
	private float?[] toStopByBrakes = new float?[9];
	private float?[] toBelowLimitByBrakes = new float?[9];

	private int prevATOBrake = -99, currentATOBrake = 0;

	public IWannaUseBrakeWell()
	{
		this.Text = "いい感じにブレーキをかけたい";
		this.Font = new Font("MS UI Gothic", fontSize, GraphicsUnit.Pixel);
		this.FormBorderStyle = FormBorderStyle.FixedSingle;
		this.MaximizeBox = false;
		this.ClientSize = GetSizeOnGrid(40.5f, 15.5f);
		Font doubleFont = new Font("MS UI Gothic", fontSize * 2, GraphicsUnit.Pixel);

		SuspendLayout();

		brakeInfoGroupBox = CreateControl<GroupBox>(this, 0.5f, 0.5f, 20, 11.5f);
		brakeInfoGroupBox.Text = "ブレーキ情報";
		brakeInfoAccelTitleLabel = CreateControl<Label>(brakeInfoGroupBox, 3.5f, 1, 5, 1);
		brakeInfoAccelTitleLabel.Text = "加速度";
		brakeInfoAccelTitleLabel.TextAlign = ContentAlignment.TopRight;
		brakeInfoStopDistTitleLabel = CreateControl<Label>(brakeInfoGroupBox, 8.5f, 1, 5, 1);
		brakeInfoStopDistTitleLabel.Text = "停車まで";
		brakeInfoStopDistTitleLabel.TextAlign = ContentAlignment.TopRight;
		brakeInfoLimitDistTitleLabel = CreateControl<Label>(brakeInfoGroupBox, 14.5f, 1, 5, 1);
		brakeInfoLimitDistTitleLabel.Text = "速度制限まで";
		brakeInfoLimitDistTitleLabel.TextAlign = ContentAlignment.TopRight;
		for (int i = 0; i < 9; i++) {
			brakeInfoPanels[i] = CreateControl<Panel>(brakeInfoGroupBox, 0.5f, 10 - i, 19, 1);
			brakeInfoNameLabels[i] = CreateControl<Label>(brakeInfoPanels[i], 0, 0, 3, 1);
			brakeInfoNameLabels[i].Text = i == 0 ? "N" : string.Format("B{0}", i);
			brakeInfoNameLabels[i].TextAlign = ContentAlignment.MiddleLeft;
			brakeInfoAccelLabel[i] = CreateControl<Label>(brakeInfoPanels[i], 3, 0, 5, 1);
			brakeInfoAccelLabel[i].Text = "###.## m/s²";
			brakeInfoAccelLabel[i].TextAlign = ContentAlignment.MiddleRight;
			brakeInfoStopDistLabel[i] = CreateControl<Label>(brakeInfoPanels[i], 8, 0, 5, 1);
			brakeInfoStopDistLabel[i].Text = "#####.## m";
			brakeInfoStopDistLabel[i].TextAlign = ContentAlignment.MiddleRight;
			brakeInfoLimitDistLabel[i] = CreateControl<Label>(brakeInfoPanels[i], 14, 0, 5, 1);
			brakeInfoLimitDistLabel[i].Text = "#####.## m";
			brakeInfoLimitDistLabel[i].TextAlign = ContentAlignment.MiddleRight;
		}

		trainInfoGroupBox = CreateControl<GroupBox>(this, 21, 0.5f, 19, 11.5f);
		trainInfoGroupBox.Text = "列車情報";
		float col1x = 0.5f, col1w = 9, col2x = 9.5f, col2w = 9;
		float row1y = 1, row2y = 4.5f, row3y = 8;

		currentSpeedTitleLabel = CreateControl<Label>(trainInfoGroupBox, col1x, row1y, col1w, 1);
		currentSpeedTitleLabel.Text = "現在の速度";
		currentSpeedLabel = CreateControl<Label>(trainInfoGroupBox, col1x, row1y + 1, col1w, 2);
		currentSpeedLabel.Text = "###.## m/s";
		currentSpeedLabel.Font = doubleFont;
		currentSpeedLabel.TextAlign = ContentAlignment.TopRight;

		currentAccelTitleLabel = CreateControl<Label>(trainInfoGroupBox, col2x, row1y, col2w, 1);
		currentAccelTitleLabel.Text = "現在の加速度";
		currentAccelLabel = CreateControl<Label>(trainInfoGroupBox, col2x, row1y + 1, col2w, 2);
		currentAccelLabel.Text = "###.## m/s²";
		currentAccelLabel.Font = doubleFont;
		currentAccelLabel.TextAlign = ContentAlignment.TopRight;

		currentDistanceTitleLabel = CreateControl<Label>(trainInfoGroupBox, col1x, row2y, col1w, 1);
		currentDistanceTitleLabel.Text = "停車位置まで";
		currentDistanceLabel = CreateControl<Label>(trainInfoGroupBox, col1x, row2y + 1, col1w, 2);
		currentDistanceLabel.Text = "#####.## m";
		currentDistanceLabel.Font = doubleFont;
		currentDistanceLabel.TextAlign = ContentAlignment.TopRight;

		currentStopPredictTitleLabel = CreateControl<Label>(trainInfoGroupBox, col2x, row2y, col2w, 1);
		currentStopPredictTitleLabel.Text = "停車まで (予測)";
		currentStopPredictLabel = CreateControl<Label>(trainInfoGroupBox, col2x, row2y + 1, col2w, 2);
		currentStopPredictLabel.Text = "#####.## m";
		currentStopPredictLabel.Font = doubleFont;
		currentStopPredictLabel.TextAlign = ContentAlignment.TopRight;

		currentLimitDistanceTitleLabel = CreateControl<Label>(trainInfoGroupBox, col1x, row3y, col1w, 1);
		currentLimitDistanceTitleLabel.Text = "速度制限位置まで";
		currentLimitDistanceLabel = CreateControl<Label>(trainInfoGroupBox, col1x, row3y + 1, col1w, 2);
		currentLimitDistanceLabel.Text = "#####.## m";
		currentLimitDistanceLabel.Font = doubleFont;
		currentLimitDistanceLabel.TextAlign = ContentAlignment.TopRight;

		currentBelowLimitPredictTitleLabel = CreateControl<Label>(trainInfoGroupBox, col2x, row3y, col2w, 1);
		currentBelowLimitPredictTitleLabel.Text = "速度制限充足まで (予測)";
		currentBelowLimitPredictLabel = CreateControl<Label>(trainInfoGroupBox, col2x, row3y + 1, col2w, 2);
		currentBelowLimitPredictLabel.Text = "#####.## m";
		currentBelowLimitPredictLabel.Font = doubleFont;
		currentBelowLimitPredictLabel.TextAlign = ContentAlignment.TopRight;

		configGroupBox = CreateControl<GroupBox>(this, 0.5f, 12.5f, 39.5f, 2.5f);
		configGroupBox.Text = "設定";

		float configX = 0.5f;
		useAutoBrakeCheckBox = CreateControl<CheckBox>(configGroupBox, configX, 1,7.25f, 1);
		useAutoBrakeCheckBox.Text = "自動ブレーキを使用";
		configX += 7.25f;

		accelSampleIntervalTitleLabel = CreateControl<Label>(configGroupBox, configX, 1, 7.25f, 1);
		accelSampleIntervalTitleLabel.Text = "加速度サンプリング間隔";
		accelSampleIntervalTitleLabel.TextAlign = ContentAlignment.TopRight;
		configX += 7.25f;
		accelSampleIntervalNumericUpDown = CreateControl<NumericUpDown>(configGroupBox, configX, 1, 3, 1);
		accelSampleIntervalNumericUpDown.Maximum = Decimal.MaxValue;
		accelSampleIntervalNumericUpDown.Minimum = 0;
		accelSampleIntervalNumericUpDown.Value = 300;
		accelSampleIntervalNumericUpDown.Increment = 10;
		configX += 3;
		accelSampleIntervalUnitLabel = CreateControl<Label>(configGroupBox, configX, 1, 1.25f, 1);
		accelSampleIntervalUnitLabel.Text = "ms";
		configX += 1.25f;

		noConsecutiveOperationTitleLabel = CreateControl<Label>(configGroupBox, configX, 1, 5.25f, 1);
		noConsecutiveOperationTitleLabel.Text = "連続操作制限";
		noConsecutiveOperationTitleLabel.TextAlign = ContentAlignment.TopRight;
		configX += 5.25f;
		noConsecutiveOperationNumericUpDown = CreateControl<NumericUpDown>(configGroupBox, configX, 1, 3, 1);
		noConsecutiveOperationNumericUpDown.Maximum = Decimal.MaxValue;
		noConsecutiveOperationNumericUpDown.Minimum = 0;
		noConsecutiveOperationNumericUpDown.Value = 500;
		noConsecutiveOperationNumericUpDown.Increment = 10;
		configX += 3;
		noConsecutiveOperationUnitLabel = CreateControl<Label>(configGroupBox, configX, 1, 1.25f, 1);
		noConsecutiveOperationUnitLabel.Text = "ms";
		configX += 1.25f;

		speedLimitMarginTitleLabel = CreateControl<Label>(configGroupBox, configX, 1, 5.25f, 1);
		speedLimitMarginTitleLabel.Text = "速度制限余裕";
		speedLimitMarginTitleLabel.TextAlign = ContentAlignment.TopRight;
		configX += 5.25f;
		speedLimitMarginNumericUpDown = CreateControl<NumericUpDown>(configGroupBox, configX, 1, 3, 1);
		speedLimitMarginNumericUpDown.Maximum = Decimal.MaxValue;
		speedLimitMarginNumericUpDown.Minimum = 0;
		speedLimitMarginNumericUpDown.Value = 3;
		speedLimitMarginNumericUpDown.Increment = 1;
		speedLimitMarginNumericUpDown.DecimalPlaces = 1;
		configX += 3;
		speedLimitMarginUnitLabel = CreateControl<Label>(configGroupBox, configX, 1, 2, 1);
		speedLimitMarginUnitLabel.Text = "km/h";
		configX += 2;

		ResumeLayout();

		Shown += ShownHandler;
	}

	private void ShownHandler(object sender, EventArgs e)
	{
		TrainCrewInput.Init();
		trainCrewValid = true;
		stopwatch = new Stopwatch();
		stopwatch.Start();
		timer = new Timer();
		timer.Interval = 15;
		timer.Tick += TickHandler;
		timer.Start();
	}

	private void FormClosedHandler(object sender, EventArgs e)
	{
		if (timer != null) timer.Stop();
		trainCrewValid = false;
		TrainCrewInput.SetATO_Notch(0);
		TrainCrewInput.Dispose();
	}

	private void TickHandler(object sender, EventArgs e)
	{
		if (!trainCrewValid) return;
		long currentTime = stopwatch.ElapsedMilliseconds;
		TrainState trainState = TrainCrewInput.GetTrainState();
		GameState gameState = TrainCrewInput.gameState;

		// 現在の速度と加速度の情報を取得・表示する
		float currentSpeed = trainState.Speed / 3.6f;
		currentSpeedLabel.Text = string.Format("{0:0.00} m/s", currentSpeed);
		while (speedInfoQueue.Count > 0)
		{
			if (!accelSample.HasValue ||
				currentTime - accelSampleIntervalNumericUpDown.Value >= speedInfoQueue.Peek().Time)
			{
				accelSample = speedInfoQueue.Dequeue();
			}
			else
			{
				break;
			}
		}
		SpeedInfo currentSpeedInfo = new SpeedInfo(currentTime, currentSpeed);
		speedInfoQueue.Enqueue(currentSpeedInfo);
		float? currentAccel = null;
		if (!accelSample.HasValue || accelSample.Value.Time >= currentSpeedInfo.Time)
		{
			currentAccelLabel.Text = "###.## m/s²";
		}
		else
		{
			currentAccel = (currentSpeedInfo.Speed - accelSample.Value.Speed) /
				(currentSpeedInfo.Time - accelSample.Value.Time) * 1000;
			currentAccelLabel.Text = string.Format("{0:0.00} m/s²", currentAccel);
		}

		// 停車するべき位置までの距離と、停車しそうな位置までの距離を求める
		float? distance = "停車".Equals(trainState.nextStopType) || "運転停車".Equals(trainState.nextStopType) ? (float?)trainState.nextUIDistance : null;
		if (distance.HasValue)
		{
			currentDistanceLabel.Text = string.Format("{0:0.00} m", distance);
		}
		else
		{
			currentDistanceLabel.Text = "#####.## m";
		}
		float toStop =
			currentSpeed == 0 ? 0 :
			currentAccel.HasValue && currentAccel.Value < 0 ? (currentSpeed * currentSpeed / (2 * -currentAccel.Value)) :
			Single.PositiveInfinity;
		if (toStop < 10000)
		{
			currentStopPredictLabel.Text = string.Format("{0:0.00} m", toStop);
		}
		else
		{
			currentStopPredictLabel.Text = "∞ m";
		}

		// 制限速度・制限開始までの距離、制限を満たすまでの距離を求める
		float speedLimit, speedLimitDistance;
		if (trainState.nextSpeedLimit >= 0)
		{
			speedLimit = trainState.nextSpeedLimit;
			speedLimitDistance = trainState.nextSpeedLimitDistance;
		}
		else
		{
			speedLimit = trainState.speedLimit;
			speedLimitDistance = 0;
		}
		speedLimit -= (float)speedLimitMarginNumericUpDown.Value;
		if (speedLimit <= 0) speedLimit = 0;
		speedLimit /= 3.6f;
		currentLimitDistanceLabel.Text = string.Format("{0:0.00} m", speedLimitDistance);
		float toBelowLimit;
		if (currentSpeed <= speedLimit)
		{
			toBelowLimit = 0;
		}
		else if (!currentAccel.HasValue || currentAccel.Value >= 0)
		{
			toBelowLimit = Single.PositiveInfinity;
		}
		else
		{
			toBelowLimit = (currentSpeed * currentSpeed - speedLimit * speedLimit) / (2 * -currentAccel.Value);
		}
		if (toBelowLimit < 10000)
		{
			currentBelowLimitPredictLabel.Text = string.Format("{0:0.00} m", toBelowLimit);
		}
		else
		{
			currentBelowLimitPredictLabel.Text = "∞ m";
		}

		// ブレーキの状態と、ブレーキの状態ごとの加速度の情報を求める
		int currentBrakeInput = trainState.Bnotch;
		int currentBrake = currentATOBrake < currentBrakeInput ? currentBrakeInput : currentATOBrake;
		// ブレーキの操作・状態の取得に適した状態かをチェックする
		bool currentGaming =
			gameState.gameScreen == GameScreen.MainGame ||
			gameState.gameScreen == GameScreen.MainGame_Pause;
		bool currentPaused = gameState.gameScreen == GameScreen.MainGame_Pause;
		if (trainState.Pnotch != prevPower)
		{
			prevPower = trainState.Pnotch;
			prevPowerChangedTime = currentTime;
		}
		if (currentBrake != prevBrake)
		{
			prevBrake = currentBrake;
			prevBrakeChangedTime = currentTime;
		}
		if (currentGaming != prevGaming)
		{
			prevGaming = currentGaming;
			if (currentGaming)
			{
				prevGameStartTime = currentTime;
				// ゲーム開始時、各ブレーキの情報をリセットする
				for (int i = 0; i < 9; i++)
				{
					accelByBrakes[i] = null;
					toStopByBrakes[i] = null;
					toBelowLimitByBrakes[i] = null;
				}
			}
		}
		if (currentPaused != prevPaused)
		{
			prevPaused = currentPaused;
			if (!currentPaused) prevPauseEndTime = currentTime;
		}
		long timeFromPowerChange = currentTime - prevPowerChangedTime;
		long timeFromBrakeChange = currentTime - prevBrakeChangedTime;
		long timeFromGameStart = currentTime - prevGameStartTime;
		long timeFromPauseEnd = currentTime - prevPauseEndTime;
		bool brakeAllowed =
			trainState.Pnotch == 0 && // マスコンが「切」状態
			trainState.Reverser > 0 && // レバーサが「前進」状態
			timeFromPowerChange >= noConsecutiveOperationNumericUpDown.Value; // マスコン変更直後でない
		bool brakeChangeAllowed =
			brakeAllowed &&
			timeFromBrakeChange >= noConsecutiveOperationNumericUpDown.Value && // ブレーキ変更直後でない
			currentGaming && // ゲーム中である
			!currentPaused && // ポーズ中でない
			timeFromGameStart >= noConsecutiveOperationNumericUpDown.Value && // ゲーム開始直後でない
			timeFromPauseEnd >= noConsecutiveOperationNumericUpDown.Value; // ポーズ解除直後でない
		if (currentAccel.HasValue && 0 <= currentBrake && currentBrake < 9 &&
			brakeChangeAllowed && currentSpeed >= 1)
		{
			// ブレーキの効果の安定が期待できる状態で、かつ1m/s以上で走っている場合のみ、加速度を更新する
			accelByBrakes[currentBrake] = currentAccel;
		}
		for (int i = 0; i < 9; i++)
		{
			if (i == currentBrake)
			{
				brakeInfoPanels[i].BackColor = Color.LightSalmon;
			}
			else if (i == currentBrakeInput)
			{
				brakeInfoPanels[i].BackColor = Color.Aqua;
			}
			else
			{
				brakeInfoPanels[i].BackColor = Panel.DefaultBackColor;
			}
			if (accelByBrakes[i].HasValue)
			{
				toStopByBrakes[i] =
					currentSpeed == 0 ? 0 :
					accelByBrakes[i].Value < 0 ? (currentSpeed * currentSpeed / (2 * -accelByBrakes[i].Value)) :
					Single.PositiveInfinity;
				toBelowLimitByBrakes[i] =
					currentSpeed <= speedLimit ? 0 :
					accelByBrakes[i].Value < 0 ? ((currentSpeed * currentSpeed - speedLimit * speedLimit) / (2 * -accelByBrakes[i].Value)) :
					Single.PositiveInfinity;
				brakeInfoAccelLabel[i].Text = string.Format("{0:0.00} m/s²", accelByBrakes[i]);
				brakeInfoStopDistLabel[i].Text = toStopByBrakes[i].Value < 10000 ?
					string.Format("{0:0.00} m", toStopByBrakes[i]) : "∞ m";
				brakeInfoLimitDistLabel[i].Text = toBelowLimitByBrakes[i].Value < 10000 ?
					string.Format("{0:0.00} m", toBelowLimitByBrakes[i]) : "∞ m";
			}
			else
			{
				toStopByBrakes[i] = null;
				toBelowLimitByBrakes[i] = null;
				brakeInfoAccelLabel[i].Text = "###.## m/s²";
				brakeInfoStopDistLabel[i].Text = "#####.## m";
				brakeInfoLimitDistLabel[i].Text = "#####.## m";
			}
		}

		// 自動ブレーキの判定を行う
		if (useAutoBrakeCheckBox.Checked && brakeAllowed)
		{
			if (brakeChangeAllowed)
			{
				if ((!distance.HasValue || distance.Value >= toStop) && speedLimitDistance >= toBelowLimit)
				{
					// 今のままで目標地点かそれより前に条件を満たせそう
					float? nextToStop = null, nextToBelowLimit = null;
					for (int i = currentBrake - 1; i >= 0; i--)
					{
						if (toStopByBrakes[i].HasValue && toBelowLimitByBrakes[i].HasValue)
						{
							nextToStop = toStopByBrakes[i];
							nextToBelowLimit = toBelowLimitByBrakes[i];
							break;
						}
					}
					if (nextToStop.HasValue && nextToBelowLimit.HasValue)
					{
						if (speedLimitDistance >= nextToBelowLimit.Value)
						{
							if (!distance.HasValue || distance.Value >= nextToStop.Value ||
								distance.Value - 1 > toStop)
							{
								// ブレーキを弱めても条件を満たせそうなら、弱める
								// または、今のままだと1mより手前に停車しそうなら、弱める
								currentATOBrake = currentBrake - 1;
							}
						}
					}
				}
				else
				{
					// 今のままだと過走/制限速度超過しそう
					// ブレーキを強くする (強くできる場合)
					if (currentBrake < 8)
					{
						float? nextToStop = null;
						for (int i = currentBrake + 1; i < 9; i++)
						{
							if (toStopByBrakes[i].HasValue && toBelowLimitByBrakes[i].HasValue)
							{
								nextToStop = toStopByBrakes[i];
								break;
							}
						}
						// 停車の場合、ブレーキを強めても1m以内に止まれそうまたは不明な場合のみ、強める
						if (speedLimitDistance < toBelowLimit ||
							!nextToStop.HasValue ||
							(distance.HasValue && distance.Value - 1 <= nextToStop.Value))
						{
							currentATOBrake = currentBrake + 1;
						}
					}
				}
			}
		}
		else
		{
			currentATOBrake = 0;
		}
		if (currentATOBrake != prevATOBrake)
		{
			TrainCrewInput.SetATO_Notch(-currentATOBrake);
			prevATOBrake = currentATOBrake;
		}
	}
}
