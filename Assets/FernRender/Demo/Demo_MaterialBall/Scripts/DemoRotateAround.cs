using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoRotateAround : MonoBehaviour
{
    public Transform m_parent;
    // Use this for initialization
    void Update () {
        transform.Rotate(Vector3.up, Space.Self);//自转
        transform.RotateAround(m_parent.position, m_parent.up, Time.deltaTime * 60);//公转
    }
}
