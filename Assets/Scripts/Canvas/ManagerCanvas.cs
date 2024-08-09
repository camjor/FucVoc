using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ManagerCanvas : MonoBehaviour
{   
    bool StateConection;
    //variables de incio
    public GameObject ScreenInicio;
    public GameObject nonUserMessage;
    public GameObject BotonReset2;
    //Variables del codigo StunMyChannels
    public STUNMyChannels conexionOK;
    // Start is called before the first frame update
    

    public void conexionUser(){
        //Debug.Log(conexionOK.conexionSC);
        StateConection=conexionOK.conexionSC;
        Debug.Log(StateConection);
        if(StateConection==true){

            ScreenInicio.SetActive(false);

        }else if (StateConection==false)
            {
                 nonUserMessage.SetActive(true);
                 ScreenInicio.SetActive(true);
                 BotonReset2.SetActive(true);
            }
                
    }
    public void SalirApp(){
        Application.Quit();
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
