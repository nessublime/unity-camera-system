using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
//SUMMARY//
//MonoBehaviour que utilizan los DollyCamMarker
public class DollyCamMarker : MonoBehaviour
{
    //*******************************************************************************************//
    //***********************************      ATTRIBUTES      **********************************//
    //*******************************************************************************************//
    //**********************************    MAIN ATTRIBUTES    **********************************//
    //SUMMARY//
    //Atributos principales, tipo de camara y tipo de Path
    public enum PathType{SIMPLE, SMOOTH}
    [SerializeField] public CameraMaster.CameraMode cameraMode;
    [SerializeField] public PathType pathType;

    //*********************************    CAMERA ATTRIBUTES    *********************************//
    //SUMMARY//
    //Atributos maximos y minimos necesarios para traducir la info RGB de las texturas a los valores de la camara
    //PROVISIONAL, deben ser dependientes del CameraMode seleccionado
    public Vector2 minMaxXAngle;
    public Vector2 minMaxYAngle;
    public Vector2 minMaxXOffset;
    public Vector2 minMaxYOffset;
    public Vector2 minMaxZOffset;
    public Vector2 minMaxZoom;
    public Vector2 minMaxSoftZoneValue;
    public Vector2 minMaxDeadZoneValue;

    public float cameraSmoothness;
    public float horizontalDamping;

    //*********************************    STATIC ATTRIBUTES    *********************************//
    //SUMMARY//
    //Variable estatica que almacena la instancia del DollyCamMarker actual
    public static GameObject actualDollyCamMarker;

    //*******************************************************************************************//
    //*************************************      METHODS      ***********************************//
    //*******************************************************************************************//
    private void Start(){
        if(actualDollyCamMarker != this.gameObject){
            this.gameObject.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
        }
    }




    //SUMMARY//
    //Metodo llamado por DollyCamCollider durante la colision para setear en CameraMaster la actual instancia de DollyCamMarker
    public void OnDollyTrackChange(Collider col){
        if(actualDollyCamMarker != this.gameObject){
            actualDollyCamMarker.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
            actualDollyCamMarker = this.gameObject;
            actualDollyCamMarker.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
            if(pathType == PathType.SIMPLE){CinemachinePath newPath = transform.GetChild(0).GetComponent<CinemachinePath>(); CameraMaster.SetNewCamera(this, newPath);}
            else if(pathType == PathType.SMOOTH){CinemachineSmoothPath newPath = transform.GetChild(0).GetComponent<CinemachineSmoothPath>(); CameraMaster.SetNewCamera(this, newPath);}
        }
    }
    //SUMMARY//
    //Metodo estatico que CameraMaster invoca para setear el actual DollyCamMarker cuando no es por colision
    public static void SetActualDollyCamMarker(GameObject newDCM){
        actualDollyCamMarker = newDCM;
    }


}
