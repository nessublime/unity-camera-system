using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//SUMMARY//
//Esta clase es el Monobehaviour que necesitan los GO DollyCamCollider 
public class DollyCamCollider : MonoBehaviour
{
    //*******************************************************************************************//
    //***********************************      ATTRIBUTES      **********************************//
    //*******************************************************************************************//
    //SUMMARY//
    //Estos atributos almacena los valores iniciales que recibira la camara al realizarse un cambio de camara 
    public float positionInfo;
    public float horizontalAngleInfo;
    public float verticalAngleInfo;
    public float xOffsetInfo;
    public float yOffsetInfo;
    public float zOffsetInfo;
    public float zoomInfo;
    public float softZoneInfo;
    public float deadZoneInfo;
    public float horizontalDamping;

    //*******************************************************************************************//
    //*************************************      METHODS      ***********************************//
    //*******************************************************************************************//
    //SUMMARY//
    //En el awake, cada DollyCamCollider almacena los valores iniciales
    public void Awake(){
        RaycastHit hit;
        int layer = LayerMask.GetMask("CameraData");
        Debug.DrawRay(transform.position,Vector3.down*3.5f,Color.red);
        if(Physics.Raycast(transform.position,Vector3.down,out hit,3.5f,layer)){
            Renderer rend = hit.transform.GetComponent<Renderer>();
            MeshCollider meshCollider = hit.collider as MeshCollider;
            Texture2D MainTexture = rend.material.mainTexture as Texture2D;
            Texture2D OffsetTexture = rend.materials[1].mainTexture as Texture2D;
            Texture2D LensTexture = rend.materials[2].mainTexture as Texture2D;
            Vector2 pixelUV = hit.textureCoord;
            pixelUV.x *= MainTexture.width;
            pixelUV.y *= MainTexture.height;
            positionInfo = MainTexture.GetPixel((int)pixelUV.x,(int)pixelUV.y).r;
            horizontalAngleInfo = MainTexture.GetPixel((int)pixelUV.x,(int)pixelUV.y).g;
            verticalAngleInfo = MainTexture.GetPixel((int)pixelUV.x,(int)pixelUV.y).b;
            xOffsetInfo = OffsetTexture.GetPixel((int)pixelUV.x,(int)pixelUV.y).r;
            yOffsetInfo = OffsetTexture.GetPixel((int)pixelUV.x,(int)pixelUV.y).g;
            zOffsetInfo = OffsetTexture.GetPixel((int)pixelUV.x,(int)pixelUV.y).b;
            zoomInfo = LensTexture.GetPixel((int)pixelUV.x,(int)pixelUV.y).r;
            softZoneInfo = LensTexture.GetPixel((int)pixelUV.x,(int)pixelUV.y).g;
            deadZoneInfo = LensTexture.GetPixel((int)pixelUV.x,(int)pixelUV.y).b;
        }
        horizontalDamping = transform.parent.parent.GetComponent<DollyCamMarker>().horizontalDamping;
    }
    //SUMMARY//
    //Setea los valores iniciales de la instancia DollyCamCOllider colisionada en CameraMaster
    void OnTriggerEnter(Collider col){
        CameraMaster.SetParameters(positionInfo,horizontalAngleInfo,verticalAngleInfo,xOffsetInfo,yOffsetInfo,zOffsetInfo,zoomInfo,softZoneInfo,deadZoneInfo, this.transform.GetChild(0).transform);
        CameraMaster.camCollider = this.gameObject;
        transform.parent.parent.GetComponent<DollyCamMarker>().OnDollyTrackChange(col);
    }
}
