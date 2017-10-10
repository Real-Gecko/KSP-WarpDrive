using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpDrive
{
	public class AlcubierreDrive: PartModule
	{
		[KSPField(isPersistant = true)]
		internal bool inWarp;

		[KSPField(isPersistant = true, guiActive = true, guiName = "Upgraded")]
		internal bool isUpgraded;

		[KSPField(isPersistant = true)]
		internal int currentFactor = -1;

		[KSPField(isPersistant = true)]
		internal string serialisedWarpVector = "";

		[KSPField(isPersistant = true)]
		internal bool containmentField = false;

		[KSPField(isPersistant = true)]
		public double lastTime = 0;

		[KSPField(isPersistant = false)]
		public float innerRadius;

		[KSPField(isPersistant = false)]
		public float outerRadius;

		[KSPField(isPersistant = false)]
		public string upgradeTechReq;

		[KSPField(isPersistant = true)]
		public bool launched = false;

		private PartResourceDefinition emResource;
		private PartResourceDefinition ecResource;

		private List<AlcubierreDrive> alcubierreDrives;
		private WarpFX fx;

		private bool alarm = false;

		internal int instanceId;

		internal bool isSlave;

		internal double gravityPull;
		internal double speedLimit;
		internal double drivesTotalPower;
		internal double drivesEfficiencyRatio;
		internal double minimumRequiredExoticMatter;
		internal double requiredForCurrentFactor;
		internal double requiredForMaximumFactor;

		private double magnitudeDiff;
		private double magnitudeChange;

		private Vector3d partHeading;
		private Vector3d warpVector;
		private Vector3d previousPartHeading;

		private double[] warpFactors = { 0.01, 0.016, 0.025, 0.04, 0.063, 0.1, 0.16, 0.25, 0.40, 0.63, 1.0, 1.6, 2.5, 4.0, 6.3, 10, 16, 25, 40, 63, 100, 160, 250, 400, 630, 1000 };

		private int lowEnergyFactor;
		private int maximumFactor;
		private int previousFactor;

		private bool vesselWasInOuterspace;
		private bool sceneLoaded;

		// Media stuff
		private AudioSource containmentSound;
		private AudioSource alarmSound;
		private AudioSource warpSound;

		internal double SelectedSpeed {
			get { return warpFactors [currentFactor]; }
		}

		internal double MaxAllowedSpeed {
			get { return warpFactors [maximumFactor]; }
		}

		public override void OnStart(PartModule.StartState state) {
			if (state == StartState.Editor)
				return;

			emResource = PartResourceLibrary.Instance.GetDefinition ("ExoticMatter");
			ecResource = PartResourceLibrary.Instance.GetDefinition ("ElectricCharge");

			instanceId = GetInstanceID ();

			if(!launched) {
				if (Utils.hasTech (upgradeTechReq)) {
					isUpgraded = true;
				}
				launched = true;
			}

			lowEnergyFactor = warpFactors.IndexOf (1.0f);
			if (currentFactor == -1)
				currentFactor = lowEnergyFactor;

			previousFactor = currentFactor;

			if (!isSlave) {
				alcubierreDrives = part.vessel.FindPartModulesImplementing<AlcubierreDrive> ();
				foreach (var drive in alcubierreDrives) {
					var driveId = drive.GetInstanceID ();
					if (driveId != instanceId)
						drive.isSlave = true;
				}
			}

			if (isSlave)
				return;

			fx = new WarpFX(this);
			if (inWarp)
				fx.StartFX ();

			if (serialisedWarpVector != null)
				warpVector = ConfigNode.ParseVector3D (serialisedWarpVector);

			if (lastTime == 0)
				lastTime = Planetarium.GetUniversalTime ();

			// If we were somewhere else, let's compensate EM production
			CompensateEM ();
			sceneLoaded = false;
			GameEvents.onLevelWasLoaded.Add (onLevelWasLoaded);

			LoadMedia ();

			if (containmentField && !inWarp) {
				containmentSound.Play ();
				containmentSound.loop = true;
			}

			if (inWarp) {
				warpSound.Play ();
			}
		}

		public void onLevelWasLoaded(GameScenes gameScene) {
			sceneLoaded = true;
			GameEvents.onLevelWasLoaded.Remove (onLevelWasLoaded);
		}

		public override void OnUpdate() {
			if (isSlave || HighLogic.LoadedSceneIsEditor)
				return;

			if (inWarp)
				fx.FrameUpdate ();
			// Fuck this API
			if (alarm) {
				alarmSound.Play ();
				alarm = false;
			}
		}

		public void FixedUpdate() {
			if (isSlave || HighLogic.LoadedSceneIsEditor)
				return;

			gravityPull = FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D()).magnitude;
			speedLimit = vessel.mainBody.flightGlobalsIndex != 0
				? 1 / (Math.Max(gravityPull - 0.006, 0.001) * 10)
				: 1 / gravityPull;

			if (speedLimit > warpFactors [warpFactors.Length - 1])
				speedLimit = warpFactors [warpFactors.Length - 1];

			maximumFactor = GetMaximumFactor(speedLimit);

			drivesTotalPower = 0;
			for (int i = 0; i < alcubierreDrives.Count; i++)
				drivesTotalPower += alcubierreDrives [i].part.mass * (alcubierreDrives [i].isUpgraded ? 20 : 10);
			
			drivesEfficiencyRatio = drivesTotalPower / vessel.totalMass;

			minimumRequiredExoticMatter = 100 * vessel.totalMass / drivesEfficiencyRatio;

			requiredForCurrentFactor = GetExoticMatterRequired(warpFactors[currentFactor]);
			requiredForMaximumFactor = GetExoticMatterRequired(warpFactors[maximumFactor]);

			// Fuck this API
			if (sceneLoaded)
				ProduceEM ();
			
			UpdateWarpSpeed();
		}

		public void CompensateEM() {
			double currentTime = Planetarium.GetUniversalTime ();
			double timeDelta = currentTime - lastTime;
			lastTime = currentTime;
			if (inWarp || !containmentField)
				return;

			vessel.RequestResource (part, emResource.id, -0.01 * timeDelta, true);
		}

		public void ProduceEM () {
			double currentTime = Planetarium.GetUniversalTime ();
			double timeDelta = currentTime - lastTime;
			lastTime = currentTime;
			if (inWarp)
				return;

			// Decay exotic matter
			if (!containmentField) {
				vessel.RequestResource (part, emResource.id, 100 * timeDelta, true);
				return;
			}

			double ecReturned = vessel.RequestResource (part, ecResource.id, 100 * timeDelta, true);

			// No EC, shutdown containment field
			if (ecReturned == 0) {
				ScreenMessages.PostScreenMessage ("Not enough EC for stable containment field!", 7.0f);
				ScreenMessages.PostScreenMessage ("Containment field is off, EM will decay!", 7.0f);
				StopContainment ();
			} else
				vessel.RequestResource (part, emResource.id, -0.01 * timeDelta, true);
		}

		public void UpdateWarpSpeed() {
			if (!inWarp || minimumRequiredExoticMatter <= 0) return;

			// Check this out
			if (this.vessel.altitude < this.vessel.mainBody.atmosphereDepth * 3) {
				if (vesselWasInOuterspace) {
					ScreenMessages.PostScreenMessage ("Atmosphere is too close! Dropping out of warp!", 7.0f);
					alarm = true;
					DeactivateWarpDrive ();
					return;
				}
			}
			else
				vesselWasInOuterspace = true;


			// Check for heading changes
			Vector3d newPartHeading = new Vector3d(part.transform.up.x, part.transform.up.z, part.transform.up.y);
			magnitudeDiff = (partHeading - newPartHeading).magnitude;
			magnitudeChange = (previousPartHeading - newPartHeading).magnitude;
			previousPartHeading = newPartHeading;

			bool headingChanged = magnitudeDiff > 0.05 && magnitudeChange < 0.0001;
			bool factorChanged = previousFactor != currentFactor;
			bool gravityDisbalance = currentFactor > maximumFactor;

			if (gravityDisbalance) {
				currentFactor = maximumFactor;
				if (currentFactor < lowEnergyFactor) {
					ScreenMessages.PostScreenMessage ("Gravity too strong, dropping out of warp!", 7.0f);
					alarm = true;
					DeactivateWarpDrive ();
					return;
				}
				ScreenMessages.PostScreenMessage ("Gravity pull increased, speed dropped down!", 7.0f);
			}

			if (gravityDisbalance || headingChanged || factorChanged) {
				previousFactor = currentFactor;

				vessel.GoOnRails ();
				vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel - warpVector, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());

				warpVector = newPartHeading * Constants.C * warpFactors [currentFactor];
				partHeading = newPartHeading;
				serialisedWarpVector = ConfigNode.WriteVector(warpVector);

				vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + warpVector, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
				vessel.GoOffRails ();
			}
		}

		public bool ExoticMatterDiff() {
			double previousLevel = GetExoticMatterRequired (warpFactors [previousFactor]);
			double currentLevel = GetExoticMatterRequired (warpFactors [currentFactor]);
			double emDiff = 0;

			if (currentLevel > previousLevel)
				emDiff = currentLevel - previousLevel;

			double availableExoticMatter;
			double maxExoticMatter;

			part.GetConnectedResourceTotals
			(
				emResource.id,
				out availableExoticMatter,
				out maxExoticMatter
			);

			if (availableExoticMatter < emDiff)
			{
				ScreenMessages.PostScreenMessage("Not enough Exotic Matter to change warp factor!", 7.0f);
				return false;
			}
			if (emDiff > 0) {
				part.RequestResource ("ExoticMatter", emDiff);
				ScreenMessages.PostScreenMessage (emDiff.ToString ("F3") + " Exotic Matter consumed!", 7.0f);
			}
			return true;
		}

		public void ActivateWarpDrive() {
			if (inWarp)
				return;

			if (this.vessel.altitude <= getMaxAtmosphericAltitude(this.vessel.mainBody) &&
				this.vessel.mainBody.flightGlobalsIndex != 0)
			{
				ScreenMessages.PostScreenMessage ("Cannot activate warp drive within the atmosphere!", 7.0f);
				return;
			}

			if (drivesEfficiencyRatio < 1)
			{
				ScreenMessages.PostScreenMessage ("Not enough drives power to initiate warp!", 7.0f);
				return;
			}

			double availableExoticMatter;
			double maxExoticMatter;

			part.GetConnectedResourceTotals
			(
				emResource.id,
				out availableExoticMatter,
				out maxExoticMatter
			);

			if (availableExoticMatter < requiredForCurrentFactor)
			{
				ScreenMessages.PostScreenMessage("Not enough Exotic Matter to initiate warp!", 7.0f);
				return;
			}

			InitiateWarp ();
		}

		private void InitiateWarp() {
			part.RequestResource("ExoticMatter", requiredForCurrentFactor);

			partHeading = new Vector3d(part.transform.up.x, part.transform.up.z, part.transform.up.y);
			warpVector = partHeading * Constants.C * warpFactors [currentFactor];
			serialisedWarpVector = ConfigNode.WriteVector(warpVector);

			vesselWasInOuterspace = (this.vessel.altitude > this.vessel.mainBody.atmosphereDepth * 10);

			vessel.GoOnRails();
			vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + warpVector, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
			vessel.GoOffRails();

			inWarp = true;
			previousPartHeading = partHeading;
			previousFactor = currentFactor;
			fx.StartFX ();
			containmentSound.Stop ();
			warpSound.Play ();
		}

		public void DeactivateWarpDrive() {
			if (!inWarp)
				return;

			vessel.GoOnRails();
			vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel - warpVector, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
			vessel.GoOffRails();
			inWarp = false;
			fx.StopFX ();
			warpSound.Stop ();
			if (containmentField)
				containmentSound.Play ();
		}

		public void StartContainment() {
			double ecProduction = Utils.CalculateSolarPower (vessel) + Utils.CalculateOtherPower (vessel);
			if (ecProduction < 100) {
				ScreenMessages.PostScreenMessage ("Not enough power to create stable containment field!", 7.0f);
				return;
			}
			containmentField = true;
			lastTime = Planetarium.GetUniversalTime ();
			containmentSound.Play ();
			containmentSound.loop = true;
		}

		public void StopContainment() {
			containmentField = false;
			containmentSound.Stop ();
			ScreenMessages.PostScreenMessage ("Containment field is off, EM will decay!", 7.0f);
		}

		public void ReduceFactor()
		{
			if (currentFactor == lowEnergyFactor) return;

			if (currentFactor < lowEnergyFactor)
				IncreaseFactor();
			else if (currentFactor > lowEnergyFactor)
				DecreaseFactor();
		}

		public void IncreaseFactor()
		{
			previousFactor = currentFactor;			

			currentFactor++;
			if (currentFactor > maximumFactor)
				currentFactor = maximumFactor;

			if (currentFactor >= warpFactors.Length)
				currentFactor = warpFactors.Length - 1;

			if (inWarp)
			if (!ExoticMatterDiff ())
				currentFactor = previousFactor;
		}

		public void DecreaseFactor()
		{
			previousFactor = currentFactor;			

			currentFactor--;
			if (currentFactor < 0)
				currentFactor = 0;

			if (inWarp)
			if (!ExoticMatterDiff ())
				currentFactor = previousFactor;
		}

		private int GetMaximumFactor(double speed)
		{
			int maxFactor = 0;

			for (int i = 0; i < warpFactors.Length; i++)
			{
				if (warpFactors[i] >= speed)
					return maxFactor;
				maxFactor = i;
			}
			return maxFactor;
		}

		private double GetExoticMatterRequired(double warpFactor)
		{
			var sqrtSpeed = Math.Sqrt(warpFactor);
			var powerModifier = warpFactor < 1 ? 1 / sqrtSpeed : sqrtSpeed;
			return powerModifier * minimumRequiredExoticMatter;
		}

		public static double getMaxAtmosphericAltitude(CelestialBody body)
		{
			if (!body.atmosphere) return 0;
			return body.atmosphereDepth;
		}

		internal void PlayAlarm() {
			alarmSound.Play ();
		}

		private void LoadMedia() {
			// Sounds
			containmentSound = gameObject.AddComponent<AudioSource> ();
			containmentSound.clip = GameDatabase.Instance.GetAudioClip ("WarpDrive/Sounds/containment");
			containmentSound.volume = GameSettings.SHIP_VOLUME;
			containmentSound.panStereo = 0;
			containmentSound.Stop ();

			warpSound = gameObject.AddComponent<AudioSource> ();
			warpSound.clip = GameDatabase.Instance.GetAudioClip ("WarpDrive/Sounds/warp");
			warpSound.volume = GameSettings.SHIP_VOLUME;
			warpSound.panStereo = 0;
			warpSound.loop = true;
			warpSound.Stop ();

			alarmSound = gameObject.AddComponent<AudioSource> ();
			alarmSound.clip = GameDatabase.Instance.GetAudioClip ("WarpDrive/Sounds/alarm");
			alarmSound.volume = GameSettings.SHIP_VOLUME;
			alarmSound.panStereo = 0;
			alarmSound.loop = false;
			alarmSound.Stop ();
		}
	}
}
