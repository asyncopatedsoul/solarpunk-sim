using NWH.Common.SceneManagement;
using NWH.Common.Vehicles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NWH.WheelController3D
{
    [RequireComponent(typeof(VehicleChanger))]
    public class GUIController : MonoBehaviour
    {   
        public FrictionPreset genericFrictionPreset;
        public FrictionPreset gravelFrictionPreset;
        public FrictionPreset iceFrictionPreset;
        public FrictionPreset snowFrictionPreset;
        public Text           speedText;
        public FrictionPreset tarmacDryFrictionPreset;
        public FrictionPreset tarmacWetFrictionPreset;


        private void Update()
        {
            if (Vehicle.ActiveVehicle != null)
            {
                float speedKmh = Mathf.RoundToInt(Vehicle.ActiveVehicle.Speed * 3.6f);
                speedText.text = Mathf.Abs(speedKmh).ToString();
            }
        }


        private WheelUAPI[] GetActiveVehicleWheels()
        {
            return Vehicle.ActiveVehicle.GetComponentsInChildren<WheelUAPI>();
        }


        public void AdjustFriction(FrictionPreset p)
        {
            foreach (WheelUAPI w in GetActiveVehicleWheels())
            {
                w.FrictionPreset = p;
            }
        }


        public void NextVehicle()
        {
            VehicleChanger.Instance.NextVehicle();
        }


        public void DecreaseBump()
        {
            foreach (WheelUAPI w in GetActiveVehicleWheels())
            {
                w.DamperBumpRate -= w.DamperBumpRate * 0.1f;
                w.DamperBumpRate =  Mathf.Clamp(w.DamperBumpRate, 1000f, 5000f);
            }
        }


        public void DecreaseCamber()
        {
            foreach (WheelUAPI w in GetActiveVehicleWheels())
            {
                float camber = w.Camber - 2f;
                camber = Mathf.Clamp(camber, -8f, 8f);
                w.Camber = camber;
            }
        }


        public void DecreaseRebound()
        {
            foreach (WheelUAPI w in GetActiveVehicleWheels())
            {
                w.DamperReboundRate -= w.DamperReboundRate * 0.1f;
                w.DamperReboundRate =  Mathf.Clamp(w.DamperReboundRate, 1000f, 5000f);
            }
        }


        public void DecreaseSpringLength()
        {
            foreach (WheelUAPI w in GetActiveVehicleWheels())
            {
                w.SpringMaxLength -= w.SpringMaxLength * 0.1f;
                w.SpringMaxLength =  Mathf.Clamp(w.SpringMaxLength, 0.15f, 0.6f);
            }
        }


        public void DecreaseSpringStrength()
        {
            foreach (WheelUAPI w in GetActiveVehicleWheels())
            {
                w.SpringMaxForce -= w.SpringMaxForce * 0.1f;
                w.SpringMaxForce =  Mathf.Clamp(w.SpringMaxForce, 14000f, 45000f);
            }
        }


        public void IncreaseBump()
        {
            foreach (WheelUAPI w in GetActiveVehicleWheels())
            {
                w.DamperBumpRate += w.DamperBumpRate * 0.1f;
                w.DamperBumpRate =  Mathf.Clamp(w.DamperBumpRate, 1000f, 5000f);
            }
        }


        public void IncreaseCamber()
        {
            foreach (WheelUAPI w in GetActiveVehicleWheels())
            {
                float camber = w.Camber + 2f;
                camber = Mathf.Clamp(camber, -8f, 8f);
                w.Camber = camber;
            }
        }


        public void IncreaseRebound()
        {
            foreach (WheelUAPI w in GetActiveVehicleWheels())
            {
                w.DamperReboundRate += w.DamperReboundRate * 0.1f;
                w.DamperReboundRate =  Mathf.Clamp(w.DamperReboundRate, 1000f, 5000f);
            }
        }


        public void IncreaseSpringLength()
        {
            foreach (WheelUAPI w in GetActiveVehicleWheels())
            {
                w.SpringMaxLength += w.SpringMaxLength * 0.1f;
                w.SpringMaxLength = Mathf.Clamp(w.SpringMaxLength, 0.15f, 0.6f);
            }
        }


        public void IncreaseSpringStrength()
        {
            foreach (WheelUAPI w in GetActiveVehicleWheels())
            {
                w.SpringMaxForce += w.SpringMaxForce * 0.1f;
                w.SpringMaxForce =  Mathf.Clamp(w.SpringMaxForce, 14000f, 45000f);
            }
        }


        public void LevelReset()
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }


        public void SurfaceGeneric()
        {
            AdjustFriction(genericFrictionPreset);
        }


        public void SurfaceGravel()
        {
            AdjustFriction(gravelFrictionPreset);
        }


        public void SurfaceIce()
        {
            AdjustFriction(iceFrictionPreset);
        }


        public void SurfaceSnow()
        {
            AdjustFriction(snowFrictionPreset);
        }


        public void SurfaceTarmacDry()
        {
            AdjustFriction(tarmacDryFrictionPreset);
        }


        public void SurfaceTarmacWet()
        {
            AdjustFriction(tarmacWetFrictionPreset);
        }

    }
}