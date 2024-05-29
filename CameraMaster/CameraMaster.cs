using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraMaster : Singleton<CameraMaster>
{
    //*******************************************************************************************//
    //***********************************      ATTRIBUTES      **********************************//
    //*******************************************************************************************//
    //**********************************    MAIN ATTRIBUTES    **********************************//
    public enum CameraMode{POV, LOOKAT, COMPOSER}
    //SUMMARY//
    //cameraPrefabList es la lista de prefabs de camaras virtuales a instanciar, cameraList almacena las instancias de estas camaras
    [SerializeField] private List<GameObject> cameraPrefabList;
    public static List<GameObject> cameraList = new List<GameObject>();
    //SUMMARY//
    //Actuales DollyCamMarker y camara vitual actual
    private static CinemachineVirtualCamera CVC;
    private static DollyCamMarker DCM;

    //*********************************    FLOOR ATTRIBUTES    **********************************//
    //SUMMARY//
    //Atributos que se setearan en la camara cada frame
    private static float positionInfo;
    private static float horizontalAngleInfo;
    private static float verticalAngleInfo;
    private static float xOffsetInfo;
    private static float yOffsetInfo;
    private static float zOffsetInfo;
    private static float zoomInfo;
    private static float softZoneInfo;
    private static float deadZoneInfo;

    private static float composerInfo1;
    private static float composerInfo2;
    //SUMMARY//
    //Ultimo DollyCamCollider colisionado, necesario para setear los valores de la camara durante un cambio de camara
    public static GameObject camCollider;

    //REVISAR
    private static int actualDataDCM; // Mirar que no sea variable residual

    private static bool camColliderCollision = false;
    private static bool camCollided = false;
    static float angle = 0;
    private static Vector3 last_forward = Vector3.forward;
    private static Vector3 last_right = Vector3.forward;
    private static Vector3 last_input = Vector3.forward;

    private static Transform newCameraDirection;

    //*********************************    REFERENCE ATTRIBUTES    **********************************//
    //SUMMARY//
    //Variables de referencia utilizadas en los SmoothDamp
    static float refFloat0 = 0, refFloat1 = 0, refFloat2 = 0, refFloat3 = 0, refFloat4 = 0, refFloat5 = 0, refFloat6 = 0, refFloat7 = 0, refFloat8 = 0;

    //*******************************************************************************************//
    //**************************************      MAIN      *************************************//
    //*******************************************************************************************//
    //***********************************    AWAKE & START    ***********************************//
    protected override void Awake(){
        base.Awake();
        InstantiateCameras();
    }

    //*******************************************************************************************//
    //*************************************      METHODS      ***********************************//
    //*******************************************************************************************//
    //SUMMARY//
    //Metodo que instancia las camaras virtuales necesarias en el Awake
    private void InstantiateCameras(){
        foreach(GameObject cameraPrefab in cameraPrefabList){
            GameObject camera = Instantiate(cameraPrefab, Vector3.zero, Quaternion.identity); 
            camera.transform.parent = gameObject.transform;
            camera.SetActive(false);
            cameraList.Add(camera);
            GameObject camera2 = Instantiate(cameraPrefab, Vector3.zero, Quaternion.identity); 
            camera2.transform.parent = gameObject.transform;
            camera2.SetActive(false);
            cameraList.Add(camera2);
    }}
    //SUMMARY//
    //Metodo llamado desde el GO a partir del cual se va a obtener la info de las texturas del suelo para obtener esta informacion
    public static void SetCameraInfo(Vector3 position){
        RaycastHit hit;
        int layer = LayerMask.GetMask("CameraData");
        Debug.DrawRay(position,Vector3.down*3.5f,Color.red);
        if(Physics.Raycast(position,Vector3.down,out hit,3.5f,layer)){
            actualDataDCM = hit.transform.parent.parent.GetSiblingIndex(); // esto para que?
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
    }
    //SUMMARY//
    //Metodo utilizado para Setear la nueva camara durante un cambio de escena
    public void SetDollyCamAtSceneChange(){
        DCM = GameObject.Find("PlayerSpawner").transform.GetChild(PlayerMaster.GetPlayerSpawnerIndex()).GetComponent<PlayerSpawner>().associatedDCM;
        DollyCamMarker.SetActualDollyCamMarker(DCM.gameObject);
        DCM.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
        DisableCameras();
        if(DCM.cameraMode == CameraMode.POV){CVC = cameraList[0].GetComponent<CinemachineVirtualCamera>(); EnableCamera(0);}else if(DCM.cameraMode == CameraMode.LOOKAT){CVC = cameraList[2].GetComponent<CinemachineVirtualCamera>(); EnableCamera(2);}else if(DCM.cameraMode == CameraMode.COMPOSER){CVC = cameraList[4].GetComponent<CinemachineVirtualCamera>(); EnableCamera(4);}
        if(DCM.pathType == DollyCamMarker.PathType.SIMPLE){CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_Path = DCM.transform.GetChild(0).GetComponent<CinemachinePath>();} else{CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_Path = DCM.transform.GetChild(0).GetComponent<CinemachineSmoothPath>();}
        UpdateDollyCamInfo();
        CVC.GetCinemachineComponent<CinemachineComposer>().m_HorizontalDamping = DCM.horizontalDamping;
    }
    //SUMMARY//
    //Sobrecarga de dos metodos utilizados por DollyCamMarker para setear la nueva camara 
    public static void SetNewCamera(DollyCamMarker newDCM, CinemachinePath newPath){
        CameraMode lastMode = DCM.cameraMode;
        DCM = newDCM;
        if(lastMode != DCM.cameraMode){
            DisableCameras();
            if(DCM.cameraMode == CameraMode.POV){
                CVC = cameraList[0].GetComponent<CinemachineVirtualCamera>();
                EnableCamera(0);}
            else if(DCM.cameraMode == CameraMode.LOOKAT){
                CVC = cameraList[2].GetComponent<CinemachineVirtualCamera>();
                EnableCamera(2);}
            else if(DCM.cameraMode == CameraMode.COMPOSER){
                CVC = cameraList[4].GetComponent<CinemachineVirtualCamera>();
                EnableCamera(4);CVC.GetCinemachineComponent<CinemachineComposer>().m_HorizontalDamping = DCM.horizontalDamping;
            }}
        else{
            DisableCameras();
            if(DCM.cameraMode == CameraMode.POV){
                if(CVC == cameraList[0].GetComponent<CinemachineVirtualCamera>()){
                    CVC = cameraList[1].GetComponent<CinemachineVirtualCamera>();
                    EnableCamera(1);
                }else{
                    CVC = cameraList[0].GetComponent<CinemachineVirtualCamera>();
                    EnableCamera(0);
                }
            }else if(DCM.cameraMode == CameraMode.LOOKAT){
                if(CVC == cameraList[2].GetComponent<CinemachineVirtualCamera>()){
                    CVC = cameraList[3].GetComponent<CinemachineVirtualCamera>();
                    EnableCamera(3);
                }else{
                    CVC = cameraList[2].GetComponent<CinemachineVirtualCamera>();
                    EnableCamera(2);
                }
            }
            else if(DCM.cameraMode == CameraMode.COMPOSER){
                if(CVC == cameraList[4].GetComponent<CinemachineVirtualCamera>()){
                    CVC = cameraList[5].GetComponent<CinemachineVirtualCamera>();
                    EnableCamera(5);
                    CVC.GetCinemachineComponent<CinemachineComposer>().m_HorizontalDamping = DCM.horizontalDamping;
                }else{
                    CVC = cameraList[4].GetComponent<CinemachineVirtualCamera>();
                    EnableCamera(4);
                    CVC.GetCinemachineComponent<CinemachineComposer>().m_HorizontalDamping = DCM.horizontalDamping;
                }
            }
        }
        CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_Path = newPath;
        camColliderCollision=true;
        camCollided = true;
        DollyCamCollider actualColider = camCollider.GetComponent<DollyCamCollider>();
        CameraChangeUpdateDollyCamInfo(actualColider.positionInfo, actualColider.horizontalAngleInfo, actualColider.verticalAngleInfo, actualColider.xOffsetInfo, actualColider.yOffsetInfo, actualColider.zOffsetInfo, actualColider.zoomInfo);
    }
    public static void SetNewCamera(DollyCamMarker newDCM, CinemachineSmoothPath newPath){
        CameraMode lastMode = DCM.cameraMode;
        DCM = newDCM;
        if(lastMode != DCM.cameraMode){
            DisableCameras();
            if(DCM.cameraMode == CameraMode.POV){
                CVC = cameraList[0].GetComponent<CinemachineVirtualCamera>();
                EnableCamera(0);}
            else if(DCM.cameraMode == CameraMode.LOOKAT){
                CVC = cameraList[2].GetComponent<CinemachineVirtualCamera>();
                EnableCamera(2);}
            else if(DCM.cameraMode == CameraMode.COMPOSER){
                CVC = cameraList[4].GetComponent<CinemachineVirtualCamera>();
                EnableCamera(4);
                CVC.GetCinemachineComponent<CinemachineComposer>().m_HorizontalDamping = DCM.horizontalDamping;
            }
        }
        else{
            DisableCameras();
            if(DCM.cameraMode == CameraMode.POV){
                if(CVC == cameraList[0].GetComponent<CinemachineVirtualCamera>()){
                    CVC = cameraList[1].GetComponent<CinemachineVirtualCamera>();
                    EnableCamera(1);
                }else{
                    CVC = cameraList[0].GetComponent<CinemachineVirtualCamera>();
                    EnableCamera(0);
                }
            }else if(DCM.cameraMode == CameraMode.LOOKAT){
                if(CVC == cameraList[2].GetComponent<CinemachineVirtualCamera>()){
                    CVC = cameraList[3].GetComponent<CinemachineVirtualCamera>();
                    EnableCamera(3);
                }else{
                    CVC = cameraList[2].GetComponent<CinemachineVirtualCamera>();
                    EnableCamera(2);
                }
            }
            else if(DCM.cameraMode == CameraMode.COMPOSER){
                if(CVC == cameraList[4].GetComponent<CinemachineVirtualCamera>()){
                    CVC = cameraList[5].GetComponent<CinemachineVirtualCamera>();
                    EnableCamera(5);
                    CVC.GetCinemachineComponent<CinemachineComposer>().m_HorizontalDamping = DCM.horizontalDamping;
                }else{
                    CVC = cameraList[4].GetComponent<CinemachineVirtualCamera>();
                    EnableCamera(4);
                    CVC.GetCinemachineComponent<CinemachineComposer>().m_HorizontalDamping = DCM.horizontalDamping;
                }
            }
        }
        CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_Path = newPath;
        camColliderCollision=true;
        camCollided = true;
        DollyCamCollider actualColider = camCollider.GetComponent<DollyCamCollider>();
        CameraChangeUpdateDollyCamInfo(actualColider.positionInfo, actualColider.horizontalAngleInfo, actualColider.verticalAngleInfo, actualColider.xOffsetInfo, actualColider.yOffsetInfo, actualColider.zOffsetInfo, actualColider.zoomInfo);
    }
    //SUMMARY//
    //Utilizados durante los cambios de camara intraescena para corregir errores 
    public static void DisableCameras(){foreach(GameObject camera in cameraList){camera.SetActive(false);}}
    public static void EnableCamera(int index){cameraList[index].SetActive(true);}
    //SUMMARY//
    // Metodos utilizados para actualizar los valores de la camara de forma instantanea, smooth o durante un cambio de camara.
    public static void SmoothUpdateDollyCamInfo(){
        int waypointNumbers;
        if(DCM.pathType == DollyCamMarker.PathType.SIMPLE){waypointNumbers = DCM.transform.GetChild(0).GetComponent<CinemachinePath>().m_Waypoints.Length;}
        else{waypointNumbers = DCM.transform.GetChild(0).GetComponent<CinemachineSmoothPath>().m_Waypoints.Length;}
        switch(DCM.cameraMode){
            case CameraMode.POV:
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_ZDamping = 0.1f;
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition, (waypointNumbers-1) * positionInfo, ref refFloat0, DCM.cameraSmoothness);
                CVC.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.Value = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.Value, Mathf.Lerp(DCM.minMaxYAngle.x,DCM.minMaxYAngle.y,horizontalAngleInfo), ref refFloat1, DCM.cameraSmoothness);
                CVC.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.Value = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.Value, Mathf.Lerp(DCM.minMaxXAngle.x,DCM.minMaxXAngle.y,verticalAngleInfo), ref refFloat2, DCM.cameraSmoothness);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.x = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.x,Mathf.Lerp(DCM.minMaxXOffset.x,DCM.minMaxXOffset.y,xOffsetInfo), ref refFloat3, DCM.cameraSmoothness);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.y = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.y,Mathf.Lerp(DCM.minMaxYOffset.x,DCM.minMaxYOffset.y,yOffsetInfo), ref refFloat4, DCM.cameraSmoothness);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.z = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.z,Mathf.Lerp(DCM.minMaxZOffset.x,DCM.minMaxZOffset.y,zOffsetInfo), ref refFloat5, DCM.cameraSmoothness);
                CVC.m_Lens.FieldOfView = Mathf.SmoothDamp(CVC.m_Lens.FieldOfView,Mathf.Lerp(DCM.minMaxZoom.x,DCM.minMaxZoom.y,zoomInfo),ref refFloat6,DCM.cameraSmoothness);
                break;
            case CameraMode.LOOKAT:
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_ZDamping = 0.1f;
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition, (waypointNumbers-1) * positionInfo, ref refFloat0, DCM.cameraSmoothness);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.x = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.x,Mathf.Lerp(DCM.minMaxXOffset.x,DCM.minMaxXOffset.y,xOffsetInfo), ref refFloat3, DCM.cameraSmoothness);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.y = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.y,Mathf.Lerp(DCM.minMaxYOffset.x,DCM.minMaxYOffset.y,yOffsetInfo), ref refFloat4, DCM.cameraSmoothness);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.z = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.z,Mathf.Lerp(DCM.minMaxZOffset.x,DCM.minMaxZOffset.y,zOffsetInfo), ref refFloat5, DCM.cameraSmoothness);
                CVC.m_Lens.FieldOfView = Mathf.SmoothDamp(CVC.m_Lens.FieldOfView,Mathf.Lerp(DCM.minMaxZoom.x,DCM.minMaxZoom.y,zoomInfo),ref refFloat6,DCM.cameraSmoothness);
                break;
            case CameraMode.COMPOSER:
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_ZDamping = 0.1f;
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition, (waypointNumbers-1) * positionInfo, ref refFloat0, DCM.cameraSmoothness);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.x = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.x,Mathf.Lerp(DCM.minMaxXOffset.x,DCM.minMaxXOffset.y,xOffsetInfo), ref refFloat3, DCM.cameraSmoothness);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.y = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.y,Mathf.Lerp(DCM.minMaxYOffset.x,DCM.minMaxYOffset.y,yOffsetInfo), ref refFloat4, DCM.cameraSmoothness);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.z = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.z,Mathf.Lerp(DCM.minMaxZOffset.x,DCM.minMaxZOffset.y,zOffsetInfo), ref refFloat5, DCM.cameraSmoothness);
                CVC.m_Lens.FieldOfView = Mathf.SmoothDamp(CVC.m_Lens.FieldOfView,Mathf.Lerp(DCM.minMaxZoom.x,DCM.minMaxZoom.y,zoomInfo),ref refFloat6,DCM.cameraSmoothness);
                //Composer things
                CVC.GetCinemachineComponent<CinemachineComposer>().m_SoftZoneWidth = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineComposer>().m_SoftZoneWidth,Mathf.Lerp(DCM.minMaxSoftZoneValue.x,DCM.minMaxSoftZoneValue.y, softZoneInfo),ref refFloat7, DCM.cameraSmoothness);
                CVC.GetCinemachineComponent<CinemachineComposer>().m_DeadZoneWidth = Mathf.SmoothDamp(CVC.GetCinemachineComponent<CinemachineComposer>().m_DeadZoneWidth,Mathf.Lerp(DCM.minMaxDeadZoneValue.x,DCM.minMaxDeadZoneValue.y, deadZoneInfo),ref refFloat8, DCM.cameraSmoothness);
                break;       
        }    
    }
    public static void UpdateDollyCamInfo(){
        int waypointNumbers;
        if(DCM.pathType == DollyCamMarker.PathType.SIMPLE){waypointNumbers = DCM.transform.GetChild(0).GetComponent<CinemachinePath>().m_Waypoints.Length;}
        else{waypointNumbers = DCM.transform.GetChild(0).GetComponent<CinemachineSmoothPath>().m_Waypoints.Length;}
        switch(DCM.cameraMode){
            case CameraMode.POV:
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_ZDamping = 0; 
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition = (waypointNumbers-1) * positionInfo;
                CVC.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.Value = Mathf.Lerp(DCM.minMaxYAngle.x,DCM.minMaxYAngle.y,horizontalAngleInfo);
                CVC.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.Value = Mathf.Lerp(DCM.minMaxXAngle.x,DCM.minMaxXAngle.y,verticalAngleInfo);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.x = Mathf.Lerp(DCM.minMaxXOffset.x,DCM.minMaxXOffset.y,xOffsetInfo);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.y = Mathf.Lerp(DCM.minMaxYOffset.x,DCM.minMaxYOffset.y,yOffsetInfo);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.z = Mathf.Lerp(DCM.minMaxZOffset.x,DCM.minMaxZOffset.y,zOffsetInfo);
                CVC.m_Lens.FieldOfView = Mathf.Lerp(DCM.minMaxZoom.x,DCM.minMaxZoom.y,zoomInfo);
                break;
            case CameraMode.LOOKAT:
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_ZDamping = 0; 
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition = (waypointNumbers-1) * positionInfo;
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.x = Mathf.Lerp(DCM.minMaxXOffset.x,DCM.minMaxXOffset.y,xOffsetInfo);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.y = Mathf.Lerp(DCM.minMaxYOffset.x,DCM.minMaxYOffset.y,yOffsetInfo);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.z = Mathf.Lerp(DCM.minMaxZOffset.x,DCM.minMaxZOffset.y,zOffsetInfo);
                CVC.m_Lens.FieldOfView = Mathf.Lerp(DCM.minMaxZoom.x,DCM.minMaxZoom.y,zoomInfo);
                break;
            case CameraMode.COMPOSER:
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_ZDamping = 0; 
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition = (waypointNumbers-1) * positionInfo;
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.x = Mathf.Lerp(DCM.minMaxXOffset.x,DCM.minMaxXOffset.y,xOffsetInfo);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.y = Mathf.Lerp(DCM.minMaxYOffset.x,DCM.minMaxYOffset.y,yOffsetInfo);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.z = Mathf.Lerp(DCM.minMaxZOffset.x,DCM.minMaxZOffset.y,zOffsetInfo);
                CVC.m_Lens.FieldOfView = Mathf.Lerp(DCM.minMaxZoom.x,DCM.minMaxZoom.y,zoomInfo);
                //Composer things
                CVC.GetCinemachineComponent<CinemachineComposer>().m_SoftZoneWidth = Mathf.Lerp(DCM.minMaxSoftZoneValue.x,DCM.minMaxSoftZoneValue.y, softZoneInfo);
                CVC.GetCinemachineComponent<CinemachineComposer>().m_DeadZoneWidth = Mathf.Lerp(DCM.minMaxDeadZoneValue.x,DCM.minMaxDeadZoneValue.y, deadZoneInfo);
                break;    
        } 
    }
    public static void CameraChangeUpdateDollyCamInfo(float positionInfox, float horizontalAngleInfox, float verticalAngleInfox, float xOffsetInfox, float yOffsetInfox, float zOffsetInfox, float zoomInfox){
        int waypointNumbers;
        if(DCM.pathType == DollyCamMarker.PathType.SIMPLE){waypointNumbers = DCM.transform.GetChild(0).GetComponent<CinemachinePath>().m_Waypoints.Length;}
        else{waypointNumbers = DCM.transform.GetChild(0).GetComponent<CinemachineSmoothPath>().m_Waypoints.Length;}
        switch(DCM.cameraMode){
            case CameraMode.POV:
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_ZDamping = 0; 
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition = (waypointNumbers-1) * positionInfox;
                CVC.GetCinemachineComponent<CinemachinePOV>().m_HorizontalAxis.Value = Mathf.Lerp(DCM.minMaxYAngle.x,DCM.minMaxYAngle.y,horizontalAngleInfox);
                CVC.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.Value = Mathf.Lerp(DCM.minMaxXAngle.x,DCM.minMaxXAngle.y,verticalAngleInfox);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.x = Mathf.Lerp(DCM.minMaxXOffset.x,DCM.minMaxXOffset.y,xOffsetInfox);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.y = Mathf.Lerp(DCM.minMaxYOffset.x,DCM.minMaxYOffset.y,yOffsetInfox);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.z = Mathf.Lerp(DCM.minMaxZOffset.x,DCM.minMaxZOffset.y,zOffsetInfox);
                CVC.m_Lens.FieldOfView = Mathf.Lerp(DCM.minMaxZoom.x,DCM.minMaxZoom.y,zoomInfox);
                break;
            case CameraMode.LOOKAT:
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_ZDamping = 0; 
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition = (waypointNumbers-1) * positionInfox;
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.x = Mathf.Lerp(DCM.minMaxXOffset.x,DCM.minMaxXOffset.y,xOffsetInfox);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.y = Mathf.Lerp(DCM.minMaxYOffset.x,DCM.minMaxYOffset.y,yOffsetInfox);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.z = Mathf.Lerp(DCM.minMaxZOffset.x,DCM.minMaxZOffset.y,zOffsetInfox);
                CVC.m_Lens.FieldOfView = Mathf.Lerp(DCM.minMaxZoom.x,DCM.minMaxZoom.y,zoomInfox);
                break;
            case CameraMode.COMPOSER:
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_ZDamping = 0; 
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition = (waypointNumbers-1) * positionInfo;
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.x = Mathf.Lerp(DCM.minMaxXOffset.x,DCM.minMaxXOffset.y,xOffsetInfo);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.y = Mathf.Lerp(DCM.minMaxYOffset.x,DCM.minMaxYOffset.y,yOffsetInfo);
                CVC.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathOffset.z = Mathf.Lerp(DCM.minMaxZOffset.x,DCM.minMaxZOffset.y,zOffsetInfo);
                CVC.m_Lens.FieldOfView = Mathf.Lerp(DCM.minMaxZoom.x,DCM.minMaxZoom.y,zoomInfo);
                //Composer things
                //CVC.GetCinemachineComponent<CinemachineComposer>().m_TrackedObjectOffset = Mathf.Lerp("max","min", composerInfo1);
                //CVC.GetCinemachineComponent<CinemachineComposer>().m_"otra cosa" = Mathf.Lerp("max","min", composerInfo2);
                break;       
        } 
    }
    //SUMMARY//
    //Metodo llamado por DollyCamCollider para setear la info de camara en un cambio de camara intraescena
    public static void SetParameters(float positionInfox, float horizontalAngleInfox, float verticalAngleInfox, float xOffsetInfox, float yOffsetInfox, float zOffsetInfox, float zoomInfox, float softZoneInfox, float deadZoneInfox, Transform cameraDirectionx){
        positionInfo = positionInfox;
        horizontalAngleInfo = horizontalAngleInfox;
        verticalAngleInfo = verticalAngleInfox;
        xOffsetInfo = xOffsetInfox;
        yOffsetInfo = yOffsetInfox;
        zOffsetInfo = zOffsetInfox;
        zoomInfo = zoomInfox;
        softZoneInfo = softZoneInfox;
        deadZoneInfo = deadZoneInfox;
        newCameraDirection = cameraDirectionx;
    }

    public static Vector3 correctionVector;
    public static Vector3 cameraPointerForward;

    public static Vector3 FixMovementDirection(Vector3 input){
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = 0; right.y = 0; forward.Normalize(); right.Normalize();
        /*if(camCollided){correctionVector.x = -input.x; correctionVector.y = 0; correctionVector.z = 1-input.z;
            cameraPointerForward = camCollider.transform.GetChild(0).forward;
            Debug.Log(cameraPointerForward);
            camCollided = false;
        }*/
        if(camColliderCollision==false){last_forward = forward; last_right = right; last_input = input;

        }
        else if(camColliderCollision==true){
            //input = input+correctionVector;
        }
        //if(Vector3.Angle(input,last_input)>=30f || input==Vector3.zero){camColliderCollision=false;}
        
        Vector3 fixedMovement = last_forward*input.z + last_right*input.x;
        return fixedMovement;
    }








    
    //**************************************        ***************************************//
    public static Vector3 FixMovement(Vector3 input){
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        Transform cameraPointer = null;
        Quaternion rotation = Quaternion.identity;
        if(camColliderCollision == true){
            cameraPointer = camCollider.transform.GetChild(0); 
            camCollided = true;
        }
        forward.y = 0; right.y = 0; forward.Normalize(); right.Normalize();
        if(camCollided == true){
            Debug.Log(input);
            Debug.Log(cameraPointer.forward);
            float angle = Vector3.SignedAngle(input,cameraPointer.forward, Vector3.up);
            //Debug.Log(angle);
            rotation = Quaternion.AngleAxis(angle,Vector3.up);
            camCollided = false;
        }
        if(camColliderCollision==false){last_forward = forward; last_right = right; last_input = input;}
        else if(camColliderCollision == true){last_forward = rotation*cameraPointer.forward; last_right = rotation*cameraPointer.right;}
        if(Vector3.Angle(input,last_input)>=30f || input==Vector3.zero){camColliderCollision=false;}
        Vector3 fixedMovement = last_forward*input.z + last_right*input.x;
        return fixedMovement;
    }

}
