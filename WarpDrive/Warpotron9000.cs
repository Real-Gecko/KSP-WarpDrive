using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using KSP.IO;

namespace WarpDrive
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class Warpotron9000: MonoBehaviour
	{
		public static Warpotron9000 Instance;

		private PluginConfiguration config;
		private bool gamePaused;
		private AlcubierreDrive masterDrive;

		// GUI stuff
		private ApplicationLauncherButton appLauncherButton;
		private IButton toolbarButton;
		private bool guiVisible;
		private bool maximized;
		private bool refresh;
		private bool globalHidden;
		private Rect windowRect;
		private int guiId;
		private bool useToolbar;
		private const ulong lockMask = 900719925474097919;

		/// <summary>
		/// Kinda constructor
		/// </summary>
		public void Awake()
		{
			if (Instance != null) {
				Destroy (this);
				return;
			}
			Instance = this;
		}

		/// <summary>
		/// Executed once after Awake
		/// </summary>
		public void Start()
		{
			guiVisible = false;
			globalHidden = false;
			gamePaused = false;
			refresh = true;

			guiId = GUIUtility.GetControlID (FocusType.Passive);
			config = PluginConfiguration.CreateForType<Warpotron9000> ();
			config.load ();

			windowRect = config.GetValue<Rect> ("windowRect", new Rect (0, 0, 300, 400));
			useToolbar = config.GetValue<bool> ("useToolbar", false);
			maximized = config.GetValue<bool> ("maximized", false);

			GameEvents.onGUIApplicationLauncherReady.Add(onGUIApplicationLauncherReady);
			GameEvents.onLevelWasLoaded.Add(onLevelWasLoaded);
			GameEvents.onVesselChange.Add(onVesselChange);
			GameEvents.onHideUI.Add(onHideUI);
			GameEvents.onShowUI.Add(onShowUI);
			GameEvents.onGamePause.Add (onGamePause);
			GameEvents.onGameUnpause.Add (onGameUnpause);
		}

		/// <summary>
		/// Hail to The King, baby
		/// </summary>
		public void OnDestroy()
		{
			GameEvents.onGUIApplicationLauncherReady.Remove (onGUIApplicationLauncherReady);
			GameEvents.onLevelWasLoaded.Remove(onLevelWasLoaded);
			GameEvents.onVesselChange.Remove (onVesselChange);
			GameEvents.onHideUI.Remove (onHideUI);
			GameEvents.onShowUI.Remove (onShowUI);
			GameEvents.onGamePause.Remove (onGamePause);
			GameEvents.onGameUnpause.Remove (onGameUnpause);

			UnlockControls ();
			DestroyLauncher ();

			config.SetValue ("windowRect", windowRect);
			config.SetValue ("useToolbar", useToolbar);
			config.SetValue ("maximized", maximized);
			config.save ();

			if (Instance == this)
				Instance = null;
		}

		private ControlTypes LockControls()
		{
			return InputLockManager.SetControlLock ((ControlTypes)lockMask, this.name);
		}

		private void UnlockControls()
		{
			InputLockManager.RemoveControlLock(this.name);
		}

		public void onGUIApplicationLauncherReady()
		{
			CreateLauncher ();
		}

		public void onLevelWasLoaded(GameScenes scene)
		{
			onVesselChange(FlightGlobals.ActiveVessel);
		}

		public void onGamePause() {
			gamePaused = true;
			UnlockControls ();
		}

		public void onGameUnpause() {
			gamePaused = false;
		}

		private void onHideUI()
		{
			globalHidden = true;
			UnlockControls ();
		}

		private void onShowUI()
		{
			globalHidden = false;
		}

		public void onVesselChange(Vessel vessel)
		{
			masterDrive = vessel.FindPartModulesImplementing<AlcubierreDrive> ().Find (t => !t.isSlave);
		}

		public void onAppTrue()
		{
			guiVisible = true;
		}

		public void onAppFalse()
		{
			guiVisible = false;
			UnlockControls ();
		}

		public void onToggle() {
			guiVisible = !guiVisible;
			if (!guiVisible) {
				UnlockControls ();
			}
		}

		private void CreateLauncher() {
			if (ToolbarManager.ToolbarAvailable && useToolbar) {
				toolbarButton = ToolbarManager.Instance.add ("Warpotron9000", "AppLaunch");
				toolbarButton.TexturePath = "WarpDrive/Textures/warpdrive-icon-toolbar";
				toolbarButton.ToolTip = "Warpotron 9000";
				toolbarButton.Visible = true;
				toolbarButton.OnClick += (ClickEvent e) => {
					onToggle ();
				};
			} else if (appLauncherButton == null) {
				appLauncherButton = ApplicationLauncher.Instance.AddModApplication (
					onAppTrue,
					onAppFalse,
					null,
					null,
					null,
					null,
					ApplicationLauncher.AppScenes.FLIGHT |
					ApplicationLauncher.AppScenes.MAPVIEW,
					GameDatabase.Instance.GetTexture ("WarpDrive/Textures/warpdrive-icon", false)
				);
			}
		}

		private void DestroyLauncher() {
			if (appLauncherButton != null) {
				ApplicationLauncher.Instance.RemoveModApplication (appLauncherButton);
				appLauncherButton = null;
			}

			if (toolbarButton != null) {
				toolbarButton.Destroy ();
				toolbarButton = null;
			}
		}

		public void Update()
		{
			if (gamePaused)
				return;
		}

		public void OnGUI()
		{
			if (gamePaused || globalHidden || !guiVisible)
				return;

			if (refresh) {
				windowRect.height = 0;
				refresh = false;
			}

			windowRect = Layout.Window (
				guiId,
				windowRect,
				DrawGUI,
				"Warpotron 9000",
				GUILayout.ExpandWidth(true),
				GUILayout.ExpandHeight(true)
			);

			if (windowRect.Contains (Event.current.mousePosition)) {
				LockControls ();
			} else {
				UnlockControls ();
			}
		}

		public void DrawGUI(int guiId)
		{
			if (masterDrive == null) {
				guiVisible = false;
				return;
			}

			GUILayout.BeginVertical ();
			Layout.LabelAndText ("Upgrade Status", masterDrive.isUpgraded ? "Butterfly" : "Snail");
			Layout.LabelAndText ("Current Gravity Force", masterDrive.gravityPull.ToString("F3") + " g");
			Layout.LabelAndText ("Speed Restricted by G", masterDrive.speedLimit.ToString("F3") + " C");
			Layout.LabelAndText ("Current Speed Factor", masterDrive.SelectedSpeed.ToString("F3") + " C");
			Layout.LabelAndText ("Maximum Speed Factor", masterDrive.MaxAllowedSpeed.ToString("F3") + " C");

			if (maximized) {
				if (Layout.Button ("Minimize")) {
					maximized = false;
					refresh = true;
				}

				Layout.LabelAndText ("Minimal Required EM", masterDrive.minimumRequiredExoticMatter.ToString ("F3"));
				Layout.LabelAndText ("Current Required EM", masterDrive.requiredForCurrentFactor.ToString ("F3"));
				Layout.LabelAndText ("Maximum Required EM", masterDrive.requiredForMaximumFactor.ToString ("F3"));

				Layout.LabelAndText ("Current Drives Power", masterDrive.drivesTotalPower.ToString ("F3"));
				Layout.LabelAndText ("Vessel Total Mass", masterDrive.vessel.totalMass.ToString ("F3") + " tons");
				Layout.LabelAndText ("Drives Efficiency", masterDrive.drivesEfficiencyRatio.ToString ("F3"));

//				Layout.LabelAndText ("Magnitude Diff", masterDrive.magnitudeDiff.ToString ());
//				Layout.LabelAndText ("Magnitude Change", masterDrive.magnitudeChange.ToString ());
			} else if (Layout.Button ("Maximize"))
				maximized = true;

			if (Layout.Button ("alarm"))
				masterDrive.PlayAlarm ();

			if (TimeWarp.CurrentRateIndex == 0) {
				GUILayout.BeginHorizontal ();
				if (Layout.Button ("Decrease Factor", Palette.red, GUILayout.Width (141))) {
					masterDrive.DecreaseFactor ();
				}
				if (Layout.Button ("Increase Factor", Palette.green, GUILayout.Width (141))) {
					masterDrive.IncreaseFactor ();
				}
				GUILayout.EndHorizontal ();

				if (Layout.Button ("Reduce Factor", Palette.blue)) {
					masterDrive.ReduceFactor ();
				}

				if (!masterDrive.inWarp) {
					if (Layout.Button ("Activate Warp Drive", Palette.green)) {
						masterDrive.ActivateWarpDrive ();
					}
				} else if (Layout.Button ("Deactivate Warp Drive", Palette.red)) {
					masterDrive.DeactivateWarpDrive ();
				}

				if (!masterDrive.containmentField) {
					if (Layout.Button ("Activate Containment Field", Palette.green)) {
						masterDrive.StartContainment ();
					}
				} else if (Layout.Button ("Deactivate Containment Field", Palette.red)) {
					masterDrive.StopContainment ();
				}
			}

			if (Layout.Button ("Close", Palette.red))
			if (appLauncherButton != null)
				appLauncherButton.SetFalse ();
			else
				onToggle ();

			if (Layout.Button("Switch Toolbar")) {
				useToolbar = !useToolbar;
				DestroyLauncher ();
				CreateLauncher ();
			}

			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}
	}
}
