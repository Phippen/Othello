using UnityEngine;
using UnityEngine.UI;

public class ResetText : MonoBehaviour {

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnChangeEvent()
    {
        Invoke("reset", 3);
    }

    void reset()
    {
        gameObject.GetComponent<Text>().text = "";
    }
}
