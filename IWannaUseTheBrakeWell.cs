using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using TrainCrew;

class IWannaUseTheBrakeWell: Form
{
	public static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.Run(new IWannaUseTheBrakeWell());
	}

	private static RegistryKey RegistryOpenCurrentUserSubKey(string name, bool writable)
	{
		try
		{
			return Registry.CurrentUser.OpenSubKey(name, writable);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static RegistryKey RegistryCreateCurrentUserSubKey(string name)
	{
		try
		{
			return Registry.CurrentUser.CreateSubKey(name);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static string RegistryGetStringValue(RegistryKey key, string name)
	{
		try
		{
			object data = key.GetValue(name);
			if (data is string)
			{
				return (string)data;
			}
			else
			{
				return null;
			}
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static int? RegistryGetIntValue(RegistryKey key, string name)
	{
		try
		{
			object data = key.GetValue(name);
			if (data is int && (int)data >= 0)
			{
				return (int)data;
			}
			else
			{
				return null;
			}
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static void RegistrySetValue(RegistryKey key, string name, object value)
	{
		try
		{
			key.SetValue(name, value);
		}
		catch (Exception)
		{
			// 握りつぶす
		}
	}

	private static readonly string RegistrySubKeyPath = "Software\\MikeCAT\\IWannaUseTheBrakeWell";

	private static readonly string LanguageValueName = "Language";
	private static readonly string LanguageJapaneseData = "Japanese";
	private static readonly string LanguageEnglishData = "English";
	private static readonly string CarModelAutoValueName = "CarModelAuto";
	private static readonly string CarModelValueName = "CarModel";
	private static readonly string CarModel4000Data = "4000";
	private static readonly string CarModel3020Data = "3020";
	private static readonly string CarModelOtherData = "Other";

	private static readonly string UseAutoBrakeValueName = "UseAutoBrake";
	private static readonly string AutoBrakeOnlyWhenManuallyBrakingValueName = "OnlyWhenManuallyBraking";
	private static readonly string AllowUsingEBValueName = "AllowUsingEB";
	private static readonly string NoConsecutiveOperationValueName = "NoConsecutiveOperation";

	private static readonly string NoStopTooEarlyValueName = "NoStopTooEarly";
	private static readonly string AccelSampleIntervalValueName = "AccelerationSampleInterval";
	private static readonly string AccelRecordLimitValueName = "AccelerationRecordLimit";

	private static readonly string NoBelowLimitTooEarlyValueName = "NoBelowLimitTooEarly";
	private static readonly string SpeedLimitOffsetValueName = "SpeedLimitPositionOffset";
	private static readonly string SpeedLimitMarginValueName = "SpeedLimitMargin";

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

	private readonly string versionString = "";

	private Panel mainPanel;

	private MenuStrip mainMenuStrip;
	private ToolStripMenuItem languageMenuItem;
	private ToolStripMenuItem languageJapaneseMenuItem, languageEnglishMenuItem;
	private ToolStripMenuItem carModelMenuItem;
	private ToolStripMenuItem carModelAutoMenuItem;
	private ToolStripSeparator carModelSeparator;
	private ToolStripMenuItem carModel4000MenuItem, carModel3020MenuItem, carModelOtherMenuItem;

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
	private CheckBox brakeOnlyWithManualCheckBox;
	private CheckBox allowUsingEBCheckBox;
	private Label noConsecutiveOperationTitleLabel, noConsecutiveOperationUnitLabel;
	private NumericUpDown noConsecutiveOperationNumericUpDown;

	private Label noStopTooEarlyTitleLabel, noStopTooEarlyUnitLabel;
	private NumericUpDown noStopTooEarlyNumericUpDown;
	private Label accelSampleIntervalTitleLabel, accelSampleIntervalUnitLabel;
	private NumericUpDown accelSampleIntervalNumericUpDown;
	private Label accelRecordLimitTitleLabel, accelRecordLimitUnitLabel;
	private NumericUpDown accelRecordLimitNumericUpDown;

	private Label noBelowLimitTooEarlyTitleLabel, noBelowLimitTooEarlyUnitLabel;
	private NumericUpDown noBelowLimitTooEarlyNumericUpDown;
	private Label speedLimitOffsetTitleLabel, speedLimitOffsetUnitLabel;
	private NumericUpDown speedLimitOffsetNumericUpDown;
	private Label speedLimitMarginTitleLabel, speedLimitMarginUnitLabel;
	private NumericUpDown speedLimitMarginNumericUpDown;

	private Timer timer = null;
	private Stopwatch stopwatch;
	private bool trainCrewValid = false;
	private int prevPower = -99, prevBrake = -99;
	private long prevPowerChangedTime = 0, prevBrakeChangedTime = 0;
	private bool prevGaming = false, prevPaused = false;
	private long prevGameStartTime = 0, prevPauseEndTime = 0;
	private string prevCarModel = null;

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

	private struct SpeedLimit
	{
		public readonly float Limit;
		public readonly float Distance;

		public SpeedLimit(float limit, float distance)
		{
			Limit = limit;
			Distance = distance;
		}
	}

	private struct HiddenSpeedLimit
	{
		public readonly float SpeedLimit;
		public readonly float DeltaToDistance; // distance + deltaToDistance = この制限までの残り距離

		public HiddenSpeedLimit(float speedLimit, float deltaToDistance)
		{
			SpeedLimit = speedLimit;
			DeltaToDistance = deltaToDistance;
		}
	}
	private readonly List<HiddenSpeedLimit> hiddenSpeedLimits = new List<HiddenSpeedLimit>();
	private float prevDistance = 0, prevSpeedLimit = 0, prevSpeedLimitDistance = 0;

	private int prevATOBrake = -99, currentATOBrake = 0;

	public IWannaUseTheBrakeWell()
	{
		AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
		Version assemblyVersion = assemblyName.Version;
		if (assemblyVersion != null)
		{
			versionString = string.Format(
				" {0}.{1}.{2}",
				assemblyVersion.Major,
				assemblyVersion.Minor,
				assemblyVersion.Build
			);
		}

		this.Font = new Font("MS UI Gothic", fontSize, GraphicsUnit.Pixel);
		this.FormBorderStyle = FormBorderStyle.FixedSingle;
		this.MaximizeBox = false;
		this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		Font doubleFont = new Font("MS UI Gothic", fontSize * 2, GraphicsUnit.Pixel);

		SuspendLayout();

		mainMenuStrip = new MenuStrip();
		languageMenuItem = new ToolStripMenuItem();
		languageMenuItem.Text = "言語 / Language (&L)";
		languageJapaneseMenuItem = new ToolStripMenuItem();
		languageJapaneseMenuItem.Text = "日本語 (&J)";
		languageEnglishMenuItem = new ToolStripMenuItem();
		languageEnglishMenuItem.Text = "English (&E)";
		languageMenuItem.DropDownItems.Add(languageJapaneseMenuItem);
		languageMenuItem.DropDownItems.Add(languageEnglishMenuItem);
		carModelMenuItem = new ToolStripMenuItem();
		carModelAutoMenuItem = new ToolStripMenuItem();
		carModelSeparator = new ToolStripSeparator();
		carModel4000MenuItem = new ToolStripMenuItem();
		carModel4000MenuItem.Text = "4000 / 4000R (&4)";
		carModel3020MenuItem = new ToolStripMenuItem();
		carModel3020MenuItem.Text = "3020 (&3)";
		carModelOtherMenuItem = new ToolStripMenuItem();
		carModelMenuItem.DropDownItems.Add(carModelAutoMenuItem);
		carModelMenuItem.DropDownItems.Add(carModelSeparator);
		carModelMenuItem.DropDownItems.Add(carModel4000MenuItem);
		carModelMenuItem.DropDownItems.Add(carModel3020MenuItem);
		carModelMenuItem.DropDownItems.Add(carModelOtherMenuItem);
		mainMenuStrip.Items.Add(languageMenuItem);
		mainMenuStrip.Items.Add(carModelMenuItem);
		this.Controls.Add(mainMenuStrip);
		this.MainMenuStrip = mainMenuStrip;

		mainPanel = CreateControl<Panel>(this, 0, 0, 40.5f, 18.5f);
		mainPanel.Top = mainMenuStrip.Height;
		this.ClientSize = new Size(mainPanel.Width, mainMenuStrip.Height + mainPanel.Height);

		brakeInfoGroupBox = CreateControl<GroupBox>(mainPanel, 0.5f, 0.5f, 20, 11.5f);
		brakeInfoAccelTitleLabel = CreateControl<Label>(brakeInfoGroupBox, 3.5f, 1, 5, 1);
		brakeInfoAccelTitleLabel.TextAlign = ContentAlignment.TopRight;
		brakeInfoStopDistTitleLabel = CreateControl<Label>(brakeInfoGroupBox, 8.5f, 1, 5, 1);
		brakeInfoStopDistTitleLabel.TextAlign = ContentAlignment.TopRight;
		brakeInfoLimitDistTitleLabel = CreateControl<Label>(brakeInfoGroupBox, 14.5f, 1, 5, 1);
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

		trainInfoGroupBox = CreateControl<GroupBox>(mainPanel, 21, 0.5f, 19, 11.5f);
		float col1x = 0.5f, col1w = 9, col2x = 9.5f, col2w = 9;
		float row1y = 1, row2y = 4.5f, row3y = 8;

		currentSpeedTitleLabel = CreateControl<Label>(trainInfoGroupBox, col1x, row1y, col1w, 1);
		currentSpeedLabel = CreateControl<Label>(trainInfoGroupBox, col1x, row1y + 1, col1w, 2);
		currentSpeedLabel.Text = "###.## m/s";
		currentSpeedLabel.Font = doubleFont;
		currentSpeedLabel.TextAlign = ContentAlignment.TopRight;

		currentAccelTitleLabel = CreateControl<Label>(trainInfoGroupBox, col2x, row1y, col2w, 1);
		currentAccelLabel = CreateControl<Label>(trainInfoGroupBox, col2x, row1y + 1, col2w, 2);
		currentAccelLabel.Text = "###.## m/s²";
		currentAccelLabel.Font = doubleFont;
		currentAccelLabel.TextAlign = ContentAlignment.TopRight;

		currentDistanceTitleLabel = CreateControl<Label>(trainInfoGroupBox, col1x, row2y, col1w, 1);
		currentDistanceLabel = CreateControl<Label>(trainInfoGroupBox, col1x, row2y + 1, col1w, 2);
		currentDistanceLabel.Text = "#####.## m";
		currentDistanceLabel.Font = doubleFont;
		currentDistanceLabel.TextAlign = ContentAlignment.TopRight;

		currentStopPredictTitleLabel = CreateControl<Label>(trainInfoGroupBox, col2x, row2y, col2w, 1);
		currentStopPredictLabel = CreateControl<Label>(trainInfoGroupBox, col2x, row2y + 1, col2w, 2);
		currentStopPredictLabel.Text = "#####.## m";
		currentStopPredictLabel.Font = doubleFont;
		currentStopPredictLabel.TextAlign = ContentAlignment.TopRight;

		currentLimitDistanceTitleLabel = CreateControl<Label>(trainInfoGroupBox, col1x, row3y, col1w, 1);
		currentLimitDistanceLabel = CreateControl<Label>(trainInfoGroupBox, col1x, row3y + 1, col1w, 2);
		currentLimitDistanceLabel.Text = "#####.## m";
		currentLimitDistanceLabel.Font = doubleFont;
		currentLimitDistanceLabel.TextAlign = ContentAlignment.TopRight;

		currentBelowLimitPredictTitleLabel = CreateControl<Label>(trainInfoGroupBox, col2x, row3y, col2w, 1);
		currentBelowLimitPredictLabel = CreateControl<Label>(trainInfoGroupBox, col2x, row3y + 1, col2w, 2);
		currentBelowLimitPredictLabel.Text = "#####.## m";
		currentBelowLimitPredictLabel.Font = doubleFont;
		currentBelowLimitPredictLabel.TextAlign = ContentAlignment.TopRight;

		configGroupBox = CreateControl<GroupBox>(mainPanel, 0.5f, 12.5f, 39.5f, 5.5f);

		useAutoBrakeCheckBox = CreateControl<CheckBox>(configGroupBox, 0.5f, 1, 9, 1);
		brakeOnlyWithManualCheckBox = CreateControl<CheckBox>(configGroupBox, 9.5f, 1, 10, 1);
		allowUsingEBCheckBox = CreateControl<CheckBox>(configGroupBox, 19.5f, 1, 7, 1);

		noConsecutiveOperationTitleLabel = CreateControl<Label>(configGroupBox, 27, 1, 7, 1);
		noConsecutiveOperationTitleLabel.TextAlign = ContentAlignment.MiddleRight;
		noConsecutiveOperationNumericUpDown = CreateControl<NumericUpDown>(configGroupBox, 34, 1, 3, 1);
		noConsecutiveOperationNumericUpDown.Maximum = Int32.MaxValue;
		noConsecutiveOperationNumericUpDown.Minimum = 0;
		noConsecutiveOperationNumericUpDown.Value = 500;
		noConsecutiveOperationNumericUpDown.Increment = 10;
		noConsecutiveOperationUnitLabel = CreateControl<Label>(configGroupBox, 37, 1, 2, 1);
		noConsecutiveOperationUnitLabel.Text = "ms";

		noStopTooEarlyTitleLabel = CreateControl<Label>(configGroupBox, 0.5f, 2.5f, 8, 1);
		noStopTooEarlyTitleLabel.TextAlign = ContentAlignment.MiddleRight;
		noStopTooEarlyNumericUpDown = CreateControl<NumericUpDown>(configGroupBox, 8.5f, 2.5f, 4, 1);
		noStopTooEarlyNumericUpDown.Maximum = (decimal)Int32.MaxValue / 100;
		noStopTooEarlyNumericUpDown.Minimum = 0;
		noStopTooEarlyNumericUpDown.Value = 1;
		noStopTooEarlyNumericUpDown.Increment = 0.1M;
		noStopTooEarlyNumericUpDown.DecimalPlaces = 2;
		noStopTooEarlyUnitLabel = CreateControl<Label>(configGroupBox, 12.5f, 2.5f, 1.5f, 1);
		noStopTooEarlyUnitLabel.Text = "m";
		noStopTooEarlyUnitLabel.TextAlign = ContentAlignment.MiddleLeft;

		accelSampleIntervalTitleLabel = CreateControl<Label>(configGroupBox, 14, 2.5f, 8, 1);
		accelSampleIntervalTitleLabel.TextAlign = ContentAlignment.MiddleRight;
		accelSampleIntervalNumericUpDown = CreateControl<NumericUpDown>(configGroupBox, 22, 2.5f, 4, 1);
		accelSampleIntervalNumericUpDown.Maximum = Int32.MaxValue;
		accelSampleIntervalNumericUpDown.Minimum = 0;
		accelSampleIntervalNumericUpDown.Value = 300;
		accelSampleIntervalNumericUpDown.Increment = 10;
		accelSampleIntervalUnitLabel = CreateControl<Label>(configGroupBox, 26, 2.5f, 1.5f, 1);
		accelSampleIntervalUnitLabel.Text = "ms";
		accelSampleIntervalUnitLabel.TextAlign = ContentAlignment.MiddleLeft;

		accelRecordLimitTitleLabel = CreateControl<Label>(configGroupBox, 27, 2.5f, 7, 1);
		accelRecordLimitTitleLabel.TextAlign = ContentAlignment.MiddleRight;
		accelRecordLimitNumericUpDown = CreateControl<NumericUpDown>(configGroupBox, 34, 2.5f, 3, 1);
		accelRecordLimitNumericUpDown.Maximum = Int32.MaxValue;
		accelRecordLimitNumericUpDown.Minimum = 0;
		accelRecordLimitNumericUpDown.Value = 500;
		accelRecordLimitNumericUpDown.Increment = 10;
		accelRecordLimitUnitLabel = CreateControl<Label>(configGroupBox, 37, 2.5f, 2, 1);
		accelRecordLimitUnitLabel.Text = "ms";
		accelRecordLimitUnitLabel.TextAlign = ContentAlignment.MiddleLeft;

		noBelowLimitTooEarlyTitleLabel = CreateControl<Label>(configGroupBox, 0.5f, 4, 8, 1);
		noBelowLimitTooEarlyTitleLabel.TextAlign = ContentAlignment.MiddleRight;
		noBelowLimitTooEarlyNumericUpDown = CreateControl<NumericUpDown>(configGroupBox, 8.5f, 4, 4, 1);
		noBelowLimitTooEarlyNumericUpDown.Maximum = (decimal)Int32.MaxValue / 100;
		noBelowLimitTooEarlyNumericUpDown.Minimum = 0;
		noBelowLimitTooEarlyNumericUpDown.Value = 1;
		noBelowLimitTooEarlyNumericUpDown.Increment = 0.1M;
		noBelowLimitTooEarlyNumericUpDown.DecimalPlaces = 2;
		noBelowLimitTooEarlyUnitLabel = CreateControl<Label>(configGroupBox, 12.5f, 4, 1, 1);
		noBelowLimitTooEarlyUnitLabel.Text = "m";
		noBelowLimitTooEarlyUnitLabel.TextAlign = ContentAlignment.MiddleLeft;

		speedLimitOffsetTitleLabel = CreateControl<Label>(configGroupBox, 14, 4, 8, 1);
		speedLimitOffsetTitleLabel.TextAlign = ContentAlignment.MiddleRight;
		speedLimitOffsetNumericUpDown = CreateControl<NumericUpDown>(configGroupBox, 22, 4, 4, 1);
		speedLimitOffsetNumericUpDown.Maximum = (decimal)Int32.MaxValue / 100;
		speedLimitOffsetNumericUpDown.Minimum = 0;
		speedLimitOffsetNumericUpDown.Value = 1;
		speedLimitOffsetNumericUpDown.Increment = 0.1M;
		speedLimitOffsetNumericUpDown.DecimalPlaces = 2;
		speedLimitOffsetUnitLabel = CreateControl<Label>(configGroupBox, 26, 4, 1.5f, 1);
		speedLimitOffsetUnitLabel.Text = "m";
		speedLimitOffsetUnitLabel.TextAlign = ContentAlignment.MiddleLeft;

		speedLimitMarginTitleLabel = CreateControl<Label>(configGroupBox, 27, 4, 7, 1);
		speedLimitMarginTitleLabel.TextAlign = ContentAlignment.MiddleRight;
		speedLimitMarginNumericUpDown = CreateControl<NumericUpDown>(configGroupBox, 34, 4, 3, 1);
		speedLimitMarginNumericUpDown.Maximum = (decimal)Int32.MaxValue / 10;
		speedLimitMarginNumericUpDown.Minimum = 0;
		speedLimitMarginNumericUpDown.Value = 0.5M;
		speedLimitMarginNumericUpDown.Increment = 0.1M;
		speedLimitMarginNumericUpDown.DecimalPlaces = 1;
		speedLimitMarginUnitLabel = CreateControl<Label>(configGroupBox, 37, 4, 2, 1);
		speedLimitMarginUnitLabel.Text = "km/h";
		speedLimitMarginUnitLabel.TextAlign = ContentAlignment.MiddleLeft;

		ResumeLayout();

		Shown += ShownHandler;
		FormClosed += FormClosedHandler;
		languageJapaneseMenuItem.Click += LanguageMenuClickHandler;
		languageEnglishMenuItem.Click += LanguageMenuClickHandler;
		carModelAutoMenuItem.Click += CarModelAutoClickHandler;
		carModel4000MenuItem.Click += CarModelSelectorClickHandler;
		carModel3020MenuItem.Click += CarModelSelectorClickHandler;
		carModelOtherMenuItem.Click += CarModelSelectorClickHandler;

		RegistryKey key = RegistryOpenCurrentUserSubKey(RegistrySubKeyPath, false);
		if (key != null)
		{
			string languageData = RegistryGetStringValue(key, LanguageValueName);
			if (LanguageEnglishData.Equals(languageData))
			{
				languageEnglishMenuItem.Checked = true;
			}
			else
			{
				languageJapaneseMenuItem.Checked = true;
			}
			int? carModelAutoData = RegistryGetIntValue(key, CarModelAutoValueName);
			carModelAutoMenuItem.Checked = carModelAutoData.HasValue && carModelAutoData.Value != 0;
			string carModelData = RegistryGetStringValue(key, CarModelValueName);
			if (CarModel4000Data.Equals(carModelData))
			{
				CarModelSelectorClickHandler(carModel4000MenuItem, null);
			}
			else if (CarModel3020Data.Equals(carModelData))
			{
				CarModelSelectorClickHandler(carModel3020MenuItem, null);
			}
			else
			{
				CarModelSelectorClickHandler(carModelOtherMenuItem, null);
			}

			int? useAutoBrakeData = RegistryGetIntValue(key, UseAutoBrakeValueName);
			useAutoBrakeCheckBox.Checked = useAutoBrakeData.HasValue && useAutoBrakeData.Value != 0;
			int? onlyWhenManuallyBrakingData = RegistryGetIntValue(key, AutoBrakeOnlyWhenManuallyBrakingValueName);
			brakeOnlyWithManualCheckBox.Checked = onlyWhenManuallyBrakingData.HasValue && onlyWhenManuallyBrakingData.Value != 0;
			int? allowUsingEBData = RegistryGetIntValue(key, AllowUsingEBValueName);
			allowUsingEBCheckBox.Checked = allowUsingEBData.HasValue && allowUsingEBData.Value != 0;
			int? noConsecutiveOperationData = RegistryGetIntValue(key, NoConsecutiveOperationValueName);
			if (noConsecutiveOperationData.HasValue)
			{
				noConsecutiveOperationNumericUpDown.Value = noConsecutiveOperationData.Value;
			}

			int? noStopTooEarlyData = RegistryGetIntValue(key, NoStopTooEarlyValueName);
			if (noStopTooEarlyData.HasValue)
			{
				noStopTooEarlyNumericUpDown.Value = (decimal)noStopTooEarlyData.Value / 100;
			}
			int? accelSampleIntervalData = RegistryGetIntValue(key, AccelSampleIntervalValueName);
			if (accelSampleIntervalData.HasValue)
			{
				accelSampleIntervalNumericUpDown.Value = accelSampleIntervalData.Value;
			}
			int? accelRecordLimitData = RegistryGetIntValue(key, AccelRecordLimitValueName);
			if (accelRecordLimitData.HasValue)
			{
				accelRecordLimitNumericUpDown.Value = accelRecordLimitData.Value;
			}

			int? noBelowLimitTooEarlyData = RegistryGetIntValue(key, NoBelowLimitTooEarlyValueName);
			if (noBelowLimitTooEarlyData.HasValue)
			{
				noBelowLimitTooEarlyNumericUpDown.Value = (decimal)noBelowLimitTooEarlyData.Value / 100;
			}
			int? speedLimitOffsetData = RegistryGetIntValue(key, SpeedLimitOffsetValueName);
			if (speedLimitOffsetData.HasValue)
			{
				speedLimitOffsetNumericUpDown.Value = (decimal)speedLimitOffsetData.Value / 100;
			}
			int? speedLimitMarginData = RegistryGetIntValue(key, SpeedLimitMarginValueName);
			if (speedLimitMarginData.HasValue)
			{
				speedLimitMarginNumericUpDown.Value = (decimal)speedLimitMarginData.Value / 10;
			}

			key.Close();
		}
		else
		{
			languageJapaneseMenuItem.Checked = true;
			carModelAutoMenuItem.Checked = true;
			CarModelSelectorClickHandler(carModelOtherMenuItem, null);
		}

		SetControlTexts();
	}

	private void SetControlTexts()
	{
		if (languageEnglishMenuItem.Checked)
		{
			this.Text = "I wanna use the brake well" + versionString;
			carModelMenuItem.Text = "Car model (&C)";
			carModelAutoMenuItem.Text = "Auto (&A)";
			carModelOtherMenuItem.Text = "Other (&O)";
			brakeInfoGroupBox.Text = "Braking Information";
			brakeInfoAccelTitleLabel.Text = "Acceleration";
			brakeInfoStopDistTitleLabel.Text = "To stop";
			brakeInfoLimitDistTitleLabel.Text = "To speed limit";
			if (carModelOtherMenuItem.Checked)
			{
				brakeInfoNameLabels[1].Text = "Holding";
			}
			trainInfoGroupBox.Text = "Train information";
			currentSpeedTitleLabel.Text = "Current speed";
			currentAccelTitleLabel.Text = "Current acceleration";
			currentDistanceTitleLabel.Text = "Distance to stopping point";
			currentStopPredictTitleLabel.Text = "Estimated distance to stop";
			currentLimitDistanceTitleLabel.Text = "Distance before speed limit";
			currentBelowLimitPredictTitleLabel.Text = "Estimated dist. to meet limit";
			configGroupBox.Text = "Configuration";
			useAutoBrakeCheckBox.Text = "Use automatic braking";
			brakeOnlyWithManualCheckBox.Text = "Only when manually braking";
			allowUsingEBCheckBox.Text = "Allow to use EB";
			noConsecutiveOperationTitleLabel.Text = "Control repeat limit";
			noStopTooEarlyTitleLabel.Text = "Early stop limit";
			accelSampleIntervalTitleLabel.Text = "Measuring time for accel.";
			accelRecordLimitTitleLabel.Text = "Accel. recording limit";
			noBelowLimitTooEarlyTitleLabel.Text = "Early limit meeting limit";
			speedLimitOffsetTitleLabel.Text = "Speed limit pos. offset";
			speedLimitMarginTitleLabel.Text = "Speed limit margin";
		}
		else
		{
			this.Text = "いい感じにブレーキをかけたい" + versionString;
			carModelMenuItem.Text = "車種 (&C)";
			carModelAutoMenuItem.Text = "自動 (&A)";
			carModelOtherMenuItem.Text = "その他 (&O)";
			brakeInfoGroupBox.Text = "ブレーキ情報";
			brakeInfoAccelTitleLabel.Text = "加速度";
			brakeInfoStopDistTitleLabel.Text = "停車まで";
			brakeInfoLimitDistTitleLabel.Text = "速度制限まで";
			if (carModelOtherMenuItem.Checked)
			{
				brakeInfoNameLabels[1].Text = "抑速";
			}
			trainInfoGroupBox.Text = "列車情報";
			currentSpeedTitleLabel.Text = "現在の速度";
			currentAccelTitleLabel.Text = "現在の加速度";
			currentDistanceTitleLabel.Text = "停車位置まで";
			currentStopPredictTitleLabel.Text = "停車まで (予測)";
			currentLimitDistanceTitleLabel.Text = "速度制限位置まで";
			currentBelowLimitPredictTitleLabel.Text = "速度制限充足まで (予測)";
			configGroupBox.Text = "設定";
			useAutoBrakeCheckBox.Text = "自動ブレーキを使用";
			brakeOnlyWithManualCheckBox.Text = "手動ブレーキ時のみ";
			allowUsingEBCheckBox.Text = "EBの使用を許可";
			noConsecutiveOperationTitleLabel.Text = "連続操作制限";
			noStopTooEarlyTitleLabel.Text = "停車制限";
			accelSampleIntervalTitleLabel.Text = "加速度サンプリング間隔";
			accelRecordLimitTitleLabel.Text = "加速度記録制限";
			noBelowLimitTooEarlyTitleLabel.Text = "速度制限充足制限";
			speedLimitOffsetTitleLabel.Text = "速度制限位置オフセット";
			speedLimitMarginTitleLabel.Text = "速度制限余裕";
		}
	}

	private void LanguageMenuClickHandler(object sender, EventArgs e)
	{
		languageJapaneseMenuItem.Checked = false;
		languageEnglishMenuItem.Checked = false;
		((ToolStripMenuItem)sender).Checked = true;
		SetControlTexts();
	}

	private void CarModelAutoClickHandler(object sender, EventArgs e)
	{
		carModelAutoMenuItem.Checked = !carModelAutoMenuItem.Checked;
	}

	private void CarModelSelectorClickHandler(object sender, EventArgs e)
	{
		carModel4000MenuItem.Checked = false;
		carModel3020MenuItem.Checked = false;
		carModelOtherMenuItem.Checked = false;
		((ToolStripMenuItem)sender).Checked = true;
		if (sender == carModel4000MenuItem)
		{
			for (int i = 1; i <= 7; i++)
			{
				brakeInfoNameLabels[i].Text = string.Format("B{0}", i);
			}
			brakeInfoNameLabels[8].Text = "EB";
		}
		else if (sender == carModel3020MenuItem)
		{
			for (int i = 1; i <= 8; i++)
			{
				brakeInfoNameLabels[i].Text = string.Format("{0}kPa", i * 50);
			}
		}
		else
		{
			brakeInfoNameLabels[1].Text = languageEnglishMenuItem.Checked ? "Holding" : "抑速";
			for (int i = 1; i <= 6; i++)
			{
				brakeInfoNameLabels[i + 1].Text = string.Format("B{0}", i);
			}
			brakeInfoNameLabels[8].Text = "EB";
		}
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

		RegistryKey key = RegistryCreateCurrentUserSubKey(RegistrySubKeyPath);
		if (key != null)
		{
			RegistrySetValue(key, LanguageValueName,
				languageJapaneseMenuItem.Checked ? LanguageJapaneseData :
				languageEnglishMenuItem.Checked ? LanguageEnglishData :
				""
			);
			RegistrySetValue(key, CarModelAutoValueName, carModelAutoMenuItem.Checked ? 1 : 0);
			RegistrySetValue(key, CarModelValueName,
				carModel4000MenuItem.Checked ? CarModel4000Data :
				carModel3020MenuItem.Checked ? CarModel3020Data :
				carModelOtherMenuItem.Checked ? CarModelOtherData :
				""
			);

			RegistrySetValue(key, UseAutoBrakeValueName, useAutoBrakeCheckBox.Checked ? 1 : 0);
			RegistrySetValue(key, AutoBrakeOnlyWhenManuallyBrakingValueName, brakeOnlyWithManualCheckBox.Checked ? 1 : 0);
			RegistrySetValue(key, AllowUsingEBValueName, allowUsingEBCheckBox.Checked ? 1 : 0);
			RegistrySetValue(key, NoConsecutiveOperationValueName, (int)noConsecutiveOperationNumericUpDown.Value);

			RegistrySetValue(key, NoStopTooEarlyValueName, (int)(noStopTooEarlyNumericUpDown.Value * 100));
			RegistrySetValue(key, AccelSampleIntervalValueName, (int)accelSampleIntervalNumericUpDown.Value);
			RegistrySetValue(key, AccelRecordLimitValueName, (int)accelRecordLimitNumericUpDown.Value);

			RegistrySetValue(key, NoBelowLimitTooEarlyValueName, (int)(noBelowLimitTooEarlyNumericUpDown.Value * 100));
			RegistrySetValue(key, SpeedLimitOffsetValueName, (int)(speedLimitOffsetNumericUpDown.Value * 100));
			RegistrySetValue(key, SpeedLimitMarginValueName, (int)(speedLimitMarginNumericUpDown.Value * 10));

			key.Close();
		}
	}

	private static float DistanceToSatisfySpeed(float currentSpeed, float targetSpeed, float? currentAccel)
	{
		if (currentSpeed <= targetSpeed)
		{
			return 0;
		}
		else if (!currentAccel.HasValue || currentAccel.Value >= 0)
		{
			return Single.PositiveInfinity;
		}
		else
		{
			return (targetSpeed * targetSpeed - currentSpeed * currentSpeed) / (2 * currentAccel.Value);
		}
	}

	private void TickHandler(object sender, EventArgs e)
	{
		if (!trainCrewValid) return;
		long currentTime = stopwatch.ElapsedMilliseconds;
		TrainState trainState = TrainCrewInput.GetTrainState();
		GameState gameState = TrainCrewInput.gameState;

		// 車種の変化時、自動設定が有効なら設定に反映する
		string carModel = trainState.CarStates.Count > 0 ? trainState.CarStates[0].CarModel : null;
		if (carModelAutoMenuItem.Checked && carModel != null && !carModel.Equals(prevCarModel))
		{
			object newItemToCheck =
				"4000".Equals(carModel) || "4000R".Equals(carModel) ? carModel4000MenuItem :
				"3020".Equals(carModel) ? carModel3020MenuItem :
				carModelOtherMenuItem;
			CarModelSelectorClickHandler(newItemToCheck, null);
		}
		prevCarModel = carModel;

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
		float toStop = DistanceToSatisfySpeed(currentSpeed, 0, currentAccel);
		if (toStop < 10000)
		{
			currentStopPredictLabel.Text = string.Format("{0:0.00} m", toStop);
		}
		else
		{
			currentStopPredictLabel.Text = "∞ m";
		}

		// 制限速度・制限開始までの距離、制限を満たすまでの距離を求める
		List<SpeedLimit> speedLimitList = new List<SpeedLimit>();
		speedLimitList.Add(new SpeedLimit(trainState.speedLimit, 0)); // 現在の制限速度
		float firstSpeedLimit, firstSpeedLimitDistance;
		if (trainState.nextSpeedLimit >= 0)
		{
			firstSpeedLimit = trainState.nextSpeedLimit;
			firstSpeedLimitDistance = trainState.nextSpeedLimitDistance;
			speedLimitList.Add(new SpeedLimit(trainState.nextSpeedLimit, trainState.nextSpeedLimitDistance));
			// 手前の制限速度予告が奥の制限速度予告で隠れたケースを検出する
			if (prevSpeedLimit >= 0 && trainState.nextSpeedLimit >= 0 && prevDistance > trainState.nextUIDistance &&
				prevSpeedLimit >= trainState.nextSpeedLimit && prevSpeedLimitDistance < trainState.nextSpeedLimitDistance)
			{
				hiddenSpeedLimits.Add(new HiddenSpeedLimit(prevSpeedLimit, prevSpeedLimitDistance - prevDistance));
			}
			// 隠れた制限速度予告が適用されるかを判定する
			for (int i = 0; i < hiddenSpeedLimits.Count; i++)
			{
				float hiddenSpeedLimitDistance = trainState.nextUIDistance + hiddenSpeedLimits[i].DeltaToDistance;
				if (trainState.nextSpeedLimit > hiddenSpeedLimits[i].SpeedLimit || hiddenSpeedLimitDistance < 0)
				{
					// ゲーム側の制限速度がこの情報より速い (すなわち、制限がかかっていない) または、通過済
					hiddenSpeedLimits.RemoveAt(i);
					i--;
				}
				else
				{
					// 制限速度を適用する
					speedLimitList.Add(new SpeedLimit(hiddenSpeedLimits[i].SpeedLimit, hiddenSpeedLimitDistance));
					// 一番手前の制限速度を表示する
					if (hiddenSpeedLimitDistance < firstSpeedLimitDistance)
					{
						firstSpeedLimit = hiddenSpeedLimits[i].SpeedLimit;
						firstSpeedLimitDistance = hiddenSpeedLimitDistance;
					}
				}
			}
		}
		else
		{
			firstSpeedLimit = trainState.speedLimit;
			firstSpeedLimitDistance = 0;
			// 制限速度予告が出ていないので、隠れた制限速度予告も無いはず
			hiddenSpeedLimits.Clear();
		}
		float speedLimitMargin = (float)speedLimitMarginNumericUpDown.Value;
		float speedLimitOffset = (float)speedLimitOffsetNumericUpDown.Value;
		firstSpeedLimit = Math.Max(firstSpeedLimit - speedLimitMargin, 0.0f) / 3.6f;
		firstSpeedLimitDistance = Math.Max(firstSpeedLimitDistance - speedLimitOffset, 0.0f);
		for (int i = 0; i < speedLimitList.Count; i++)
		{
			speedLimitList[i] = new SpeedLimit(
				Math.Max(speedLimitList[i].Limit - speedLimitMargin, 0.0f) / 3.6f,
				Math.Max(speedLimitList[i].Distance - speedLimitOffset, 0.0f)
			);
		}
		currentLimitDistanceLabel.Text = string.Format("{0:0.00} m", firstSpeedLimitDistance);
		float toBelowFirstLimit = DistanceToSatisfySpeed(currentSpeed, firstSpeedLimit, currentAccel);
		if (toBelowFirstLimit < 10000)
		{
			currentBelowLimitPredictLabel.Text = string.Format("{0:0.00} m", toBelowFirstLimit);
		}
		else
		{
			currentBelowLimitPredictLabel.Text = "∞ m";
		}
		prevDistance = trainState.nextUIDistance;
		prevSpeedLimit = trainState.nextSpeedLimit;
		prevSpeedLimitDistance = trainState.nextSpeedLimitDistance;

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
		long timeFromLastAction =
			Math.Min(timeFromPowerChange,
			Math.Min(timeFromBrakeChange,
			Math.Min(timeFromGameStart, timeFromPauseEnd)));
		bool brakeAllowed =
			trainState.Pnotch == 0 && // マスコンが「切」状態
			trainState.Reverser > 0; // レバーサが「前進」状態
		bool normallyRunning = brakeAllowed && currentGaming && !currentPaused;
		bool brakeChangeAllowed =
			normallyRunning && // 通常の走行中 (前進、マスコン切、ゲーム中、ポーズ解除)
			timeFromLastAction >= noConsecutiveOperationNumericUpDown.Value; // 操作直後でない
		bool brakeRecordAllowed =
			normallyRunning && // 通常の走行中 (前進、マスコン切、ゲーム中、ポーズ解除)
			timeFromLastAction >= accelRecordLimitNumericUpDown.Value; // 操作直後でない
		if (currentAccel.HasValue && 0 <= currentBrake && currentBrake < 9 &&
			brakeRecordAllowed && currentSpeed >= 1)
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
				float toStopByBrake = DistanceToSatisfySpeed(currentSpeed, 0, accelByBrakes[i].Value);
				float toBelowFirstLimitByBrake = DistanceToSatisfySpeed(currentSpeed, firstSpeedLimit, accelByBrakes[i].Value);
				brakeInfoAccelLabel[i].Text = string.Format("{0:0.00} m/s²", accelByBrakes[i]);
				brakeInfoStopDistLabel[i].Text = toStopByBrake < 10000 ?
					string.Format("{0:0.00} m", toStopByBrake) : "∞ m";
				brakeInfoLimitDistLabel[i].Text = toBelowFirstLimitByBrake < 10000 ?
					string.Format("{0:0.00} m", toBelowFirstLimitByBrake) : "∞ m";
			}
			else
			{
				brakeInfoAccelLabel[i].Text = "###.## m/s²";
				brakeInfoStopDistLabel[i].Text = "#####.## m";
				brakeInfoLimitDistLabel[i].Text = "#####.## m";
			}
		}

		// 自動ブレーキの判定を行う
		if (useAutoBrakeCheckBox.Checked && brakeAllowed &&
			(!brakeOnlyWithManualCheckBox.Checked || currentBrakeInput > 0 || trainState.Pnotch < 0))
		{
			if (brakeChangeAllowed)
			{
				float noStopTooEarly = (float)noStopTooEarlyNumericUpDown.Value;
				float noBelowLimitTooEarly = (float)noBelowLimitTooEarlyNumericUpDown.Value;
				bool willStop = !distance.HasValue || distance.Value >= toStop;
				bool willMeetLimit = true;
				foreach (SpeedLimit speedLimit in speedLimitList)
				{
					willMeetLimit = willMeetLimit &&
						speedLimit.Distance >= DistanceToSatisfySpeed(currentSpeed, speedLimit.Limit, currentAccel);
				}
				if (willStop && willMeetLimit)
				{
					// 今のままで目標地点かそれより前に条件を満たせそう
					if (currentBrake > 0)
					{
						float? nextAccel = null;
						for (int i = currentBrake - 1; i >= 0; i--)
						{
							if (accelByBrakes[i].HasValue)
							{
								nextAccel = accelByBrakes[i];
								break;
							}
						}
						// ブレーキを弱めても条件を満たせそうなら、弱める
						// または、今のままだと基準より手前で停車または制限充足しそうなら、弱める
						bool shouldWeakenBrakeByStop =
							!distance.HasValue ||
							(nextAccel.HasValue && distance.Value > DistanceToSatisfySpeed(currentSpeed, 0, nextAccel)) ||
							distance.Value - noStopTooEarly > toStop;
						bool shouldWeakenBrakeBySpeedLimit = true;
						foreach (SpeedLimit speedLimit in speedLimitList)
						{
							shouldWeakenBrakeBySpeedLimit = shouldWeakenBrakeBySpeedLimit && (
									(nextAccel.HasValue ?
										// ブレーキを弱めても制限充足しそう？
										speedLimit.Distance >= DistanceToSatisfySpeed(currentSpeed, speedLimit.Limit, nextAccel) :
										// ブレーキを弱めたときの効果は不明だが、現在全ての制限をすでに充足している (減速不要)？
										DistanceToSatisfySpeed(currentSpeed, speedLimit.Limit, currentAccel) == 0
									) ||
									// 今のままだと基準より手前で制限充足しそう？
									speedLimit.Distance - noBelowLimitTooEarly > DistanceToSatisfySpeed(currentSpeed, speedLimit.Limit, currentAccel)
								);
						}
						if (shouldWeakenBrakeByStop && shouldWeakenBrakeBySpeedLimit)
						{
							currentATOBrake = currentBrake - 1;
						}
					}
				}
				else
				{
					// 今のままだと過走/制限速度超過しそう
					// ブレーキを強くする (強くできる場合)
					if (currentBrake < (allowUsingEBCheckBox.Checked || carModel3020MenuItem.Checked ? 8 : 7))
					{
						float? nextAccel = null;
						for (int i = currentBrake + 1; i < 9; i++)
						{
							if (accelByBrakes[i].HasValue)
							{
								nextAccel = accelByBrakes[i];
								break;
							}
						}
						// 過走しそうな場合、ブレーキを強めても基準内に止まれそうまたは不明な場合のみ、強める
						bool shouldStrengthenByStop = !nextAccel.HasValue ||
							(distance.HasValue && distance.Value < toStop && distance.Value - noStopTooEarly <= DistanceToSatisfySpeed(currentSpeed, 0, nextAccel));
						// 制限速度超過しそうな場合、ブレーキを強めても制限充足が基準内になりそうまたは不明な場合のみ、強める
						bool shouldStrengthenBySpeedLimit = false;
						if (nextAccel.HasValue)
						{
							foreach (SpeedLimit speedLimit in speedLimitList)
							{
								shouldStrengthenBySpeedLimit = shouldStrengthenBySpeedLimit ||
									(speedLimit.Distance < DistanceToSatisfySpeed(currentSpeed, speedLimit.Limit, currentAccel) &&
									speedLimit.Distance - noBelowLimitTooEarly <= DistanceToSatisfySpeed(currentSpeed, speedLimit.Limit, nextAccel));
							}
						}
						if (shouldStrengthenByStop || shouldStrengthenBySpeedLimit)
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
