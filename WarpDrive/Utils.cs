using System;
using System.Collections.Generic;

namespace WarpDrive
{
	public class Utils
	{
		internal static double CalculateSolarPower(Vessel vessel)
		{
			double solarPower = 0;

			List<ModuleDeployableSolarPanel> solarPanels =
				vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel> ();
			
			for (int i = 0; i < solarPanels.Count; i++) {
				ModuleDeployableSolarPanel solarPanel = solarPanels [i];
				if (solarPanel.deployState != ModuleDeployableSolarPanel.DeployState.BROKEN &&
				    solarPanel.deployState != ModuleDeployableSolarPanel.DeployState.RETRACTED &&
				    solarPanel.deployState != ModuleDeployableSolarPanel.DeployState.RETRACTING) {
					solarPower += solarPanel.flowRate;
				}
			}
			return solarPower;
		}

		internal static double CalculateOtherPower(Vessel vessel)
		{
			double otherPower = 0;
			List<ModuleGenerator> powerModules =
				vessel.FindPartModulesImplementing<ModuleGenerator> ();

			for (int i = 0; i < powerModules.Count; i++) {
				// Find standard RTGs
				ModuleGenerator powerModule = powerModules [i];
				if (powerModule.generatorIsActive || powerModule.isAlwaysActive) {
					for (int j = 0; j < powerModule.resHandler.outputResources.Count; ++j) {
						var resource = powerModule.resHandler.outputResources [j];
						if (resource.name == "ElectricCharge") {
							otherPower += resource.rate * powerModule.efficiency;
						}
					}
				}
			}

			for (int i = 0; i < vessel.parts.Count; i++) {
				var part = vessel.parts [i];
				// Search for other generators
				PartModuleList modules = part.Modules;

				for (int j = 0; j < modules.Count; j++) {
					var module = modules [j];

					// Near future fission reactors
					if (module.moduleName == "FissionGenerator") {
						otherPower += double.Parse (module.Fields.GetValue ("CurrentGeneration").ToString ());
					}
				}

				// USI reactors
				ModuleResourceConverter converterModule = part.FindModuleImplementing<ModuleResourceConverter> ();
				if (converterModule != null) {
					if (converterModule.ModuleIsActive () && converterModule.ConverterName == "Reactor") {
						for (int j = 0; j < converterModule.outputList.Count; ++j) {
							var resource = converterModule.outputList [j];
							if (resource.ResourceName == "ElectricCharge") {
								otherPower += resource.Ratio * converterModule.GetEfficiencyMultiplier ();
							}
						}
					}
				}
			}
			return otherPower;
		} // So many ifs.....

		public static bool hasTech(string techid)
		{
			if (String.IsNullOrEmpty (techid))
				return false;

			ProtoTechNode techstate = ResearchAndDevelopment.Instance.GetTechState (techid);
			if (techstate != null)
				return (techstate.state == RDTech.State.Available);
			else
				return false;
		}
	}
}

