using System;
using UnityEngine;

namespace WarpDrive
{
	public class WarpFX
	{
		internal GameObject warpTrailInner;
		internal GameObject warpTrailOuter;
		internal GameObject warpPlaneA;
		internal GameObject warpPlaneB;
		internal GameObject warpPlaneC;
		internal GameObject warpPlaneD;
		internal GameObject planesCore;
		private FlightCamera camera;

		internal WarpFX (AlcubierreDrive drive)
		{
			camera = FlightCamera.fetch;
			CreateTrail (drive);
			if (!drive.isSlave)
				CreatePlanes (drive);
		}

		internal void StartFX() {
			warpTrailInner.GetComponent<Renderer> ().enabled = true;
			warpTrailOuter.GetComponent<Renderer> ().enabled = true;
			warpPlaneA.GetComponent<Renderer> ().enabled = true;
			warpPlaneB.GetComponent<Renderer> ().enabled = true;
			warpPlaneC.GetComponent<Renderer> ().enabled = true;
			warpPlaneD.GetComponent<Renderer> ().enabled = true;
			camera.SetFoV (160);
		}

		internal void StopFX() {
			warpTrailInner.GetComponent<Renderer> ().enabled = false;
			warpTrailOuter.GetComponent<Renderer> ().enabled = false;
			warpPlaneA.GetComponent<Renderer> ().enabled = false;
			warpPlaneB.GetComponent<Renderer> ().enabled = false;
			warpPlaneC.GetComponent<Renderer> ().enabled = false;
			warpPlaneD.GetComponent<Renderer> ().enabled = false;
			camera.SetFoV (camera.fovDefault);
		}

		private void CreateTrail(AlcubierreDrive drive) {
			warpTrailInner = GameObject.CreatePrimitive (PrimitiveType.Cylinder);
			warpTrailInner.GetComponent<Collider> ().enabled = false;
			warpTrailInner.transform.parent = drive.part.transform;
			warpTrailInner.transform.up = drive.transform.up;
			warpTrailInner.transform.localScale = new Vector3 (drive.innerRadius, Constants.trailLength, drive.innerRadius);
			warpTrailInner.transform.localPosition = new Vector3 (0, 0- Constants.trailLength, 0);

			warpTrailInner.GetComponent<Renderer> ().material.shader = Shader.Find ("Unlit/Transparent");
			warpTrailInner.GetComponent<Renderer> ().material.color = new Color (255.0f, 255.0f, 255.0f, 0.75f);
			warpTrailInner.GetComponent<Renderer> ().receiveShadows = false;
			warpTrailInner.GetComponent<Renderer> ().material.mainTexture = GameDatabase.Instance.GetTexture ("WarpDrive/ParticleFX/warp_trail_inner", false);

			warpTrailInner.GetComponent<Renderer> ().enabled = false;

			warpTrailOuter = GameObject.CreatePrimitive (PrimitiveType.Cylinder);
			warpTrailOuter.GetComponent<Collider> ().enabled = false;
			warpTrailOuter.transform.parent = drive.part.transform;
			warpTrailOuter.transform.up = drive.transform.up;
			warpTrailOuter.transform.localScale = new Vector3 (drive.outerRadius, Constants.trailLength, drive.outerRadius);
			warpTrailOuter.transform.localPosition = new Vector3 (0, 0- Constants.trailLength, 0);

			warpTrailOuter.GetComponent<Renderer> ().material.shader = Shader.Find ("Unlit/Transparent");
			warpTrailOuter.GetComponent<Renderer> ().material.color = new Color (255.0f, 255.0f, 255.0f, 0.75f);
			warpTrailOuter.GetComponent<Renderer> ().receiveShadows = false;
			warpTrailOuter.GetComponent<Renderer> ().material.mainTexture = GameDatabase.Instance.GetTexture ("WarpDrive/ParticleFX/warp_trail_outer", false);

			warpTrailOuter.GetComponent<Renderer> ().enabled = false;
		}

		private void CreatePlanes(AlcubierreDrive drive) {
			planesCore = new GameObject ();
			planesCore.transform.parent = camera.transform;
			planesCore.transform.up = warpTrailInner.transform.up;
			planesCore.transform.localPosition = new Vector3 (0, 0, 0);

			warpPlaneA = GameObject.CreatePrimitive (PrimitiveType.Plane);
			warpPlaneA.GetComponent<Collider> ().enabled = false;
			warpPlaneA.transform.parent = planesCore.transform;
			warpPlaneA.transform.localScale = Vector3.one * 10000;
			warpPlaneA.transform.up = planesCore.transform.up;
			warpPlaneA.transform.localRotation = Quaternion.Euler (-90, 0, 0);
			warpPlaneA.transform.localPosition = new Vector3 (0, 0, 100);

			warpPlaneA.GetComponent<Renderer> ().material.shader = Shader.Find ("Unlit/Transparent");
			warpPlaneA.GetComponent<Renderer> ().material.color = Color.white;
			warpPlaneA.GetComponent<Renderer> ().receiveShadows = false;
			warpPlaneA.GetComponent<Renderer> ().material.mainTexture = GameDatabase.Instance.GetTexture ("WarpDrive/ParticleFX/energy", false);
			warpPlaneA.GetComponent<Renderer> ().material.renderQueue = 1001;
			warpPlaneA.GetComponent<Renderer> ().material.mainTextureScale = new Vector2 (100, 100);
			warpPlaneA.GetComponent<Renderer> ().enabled = false;

			warpPlaneB = GameObject.CreatePrimitive (PrimitiveType.Plane);
			warpPlaneB.GetComponent<Collider> ().enabled = false;
			warpPlaneB.transform.parent = planesCore.transform;
			warpPlaneB.transform.localScale = Vector3.one * 10000;
			warpPlaneB.transform.up = planesCore.transform.up;
			warpPlaneB.transform.localRotation = Quaternion.Euler (90, 0, 0);
			warpPlaneB.transform.localPosition = new Vector3 (0, 0, -100);

			warpPlaneB.GetComponent<Renderer> ().material.shader = Shader.Find ("Unlit/Transparent");
			warpPlaneB.GetComponent<Renderer> ().material.color = Color.white;
			warpPlaneB.GetComponent<Renderer> ().receiveShadows = false;
			warpPlaneB.GetComponent<Renderer> ().material.mainTexture = GameDatabase.Instance.GetTexture ("WarpDrive/ParticleFX/energy", false);
			warpPlaneB.GetComponent<Renderer> ().material.renderQueue = 1001;
			warpPlaneB.GetComponent<Renderer> ().material.mainTextureScale = new Vector2 (100, 100);
			warpPlaneB.GetComponent<Renderer> ().enabled = false;

			warpPlaneC = GameObject.CreatePrimitive (PrimitiveType.Plane);
			warpPlaneC.GetComponent<Collider> ().enabled = false;
			warpPlaneC.transform.parent = planesCore.transform;
			warpPlaneC.transform.localScale = Vector3.one * 10000;
			warpPlaneC.transform.up = planesCore.transform.up;
			warpPlaneC.transform.localRotation = Quaternion.Euler (-90, 0, 0);
			warpPlaneC.transform.localPosition = new Vector3 (0, 0, 400);

			warpPlaneC.GetComponent<Renderer> ().material.shader = Shader.Find ("Unlit/Transparent");
			warpPlaneC.GetComponent<Renderer> ().material.color = Color.white;
			warpPlaneC.GetComponent<Renderer> ().receiveShadows = false;
			warpPlaneC.GetComponent<Renderer> ().material.mainTexture = GameDatabase.Instance.GetTexture ("WarpDrive/ParticleFX/nebula", false);
			warpPlaneC.GetComponent<Renderer> ().material.renderQueue = 1000;
			warpPlaneC.GetComponent<Renderer> ().material.mainTextureScale = new Vector2 (10, 10);
			warpPlaneC.GetComponent<Renderer> ().enabled = false;

			warpPlaneD = GameObject.CreatePrimitive (PrimitiveType.Plane);
			warpPlaneD.GetComponent<Collider> ().enabled = false;
			warpPlaneD.transform.parent = planesCore.transform;
			warpPlaneD.transform.localScale = Vector3.one * 10000;
			warpPlaneD.transform.up = planesCore.transform.up;
			warpPlaneD.transform.localRotation = Quaternion.Euler (90, 0, 0);
			warpPlaneD.transform.localPosition = new Vector3 (0, 0, -400);

			warpPlaneD.GetComponent<Renderer> ().material.shader = Shader.Find ("Unlit/Transparent");
			warpPlaneD.GetComponent<Renderer> ().material.color = Color.white;
			warpPlaneD.GetComponent<Renderer> ().receiveShadows = false;
			warpPlaneD.GetComponent<Renderer> ().material.mainTexture = GameDatabase.Instance.GetTexture ("WarpDrive/ParticleFX/nebula", false);
			warpPlaneD.GetComponent<Renderer> ().material.renderQueue = 1000;
			warpPlaneD.GetComponent<Renderer> ().material.mainTextureScale = new Vector2 (10, 10);
			warpPlaneD.GetComponent<Renderer> ().enabled = false;
		}

		internal void FrameUpdate() {
			if (MapView.MapIsEnabled)
				camera.SetFoV (camera.fovDefault);
			else
				camera.SetFoV (160);

			planesCore.transform.up = warpTrailInner.transform.up;

			warpPlaneA.GetComponent<Renderer> ().material.mainTextureOffset += new Vector2 (0, -0.06f);
			warpPlaneB.GetComponent<Renderer> ().material.mainTextureOffset += new Vector2 (0, 0.06f);
			warpPlaneC.GetComponent<Renderer> ().material.mainTextureOffset += new Vector2 (0, -0.002f);
			warpPlaneD.GetComponent<Renderer> ().material.mainTextureOffset += new Vector2 (0, 0.002f);
		}
	}
}
