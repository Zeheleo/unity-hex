using UnityEngine;

public class HexUnit : MonoBehaviour
{
    void Start()
    {
        
    }
    
    void Update()
    {
        
    }

    HexCell _location;
    float _rotation;

    public HexCell Location
    {
        get
        {
            return _location;
        }

        set
        {
            Location = value;
            value.Unit = this;
            transform.localPosition = value.Position;
        }
    }

    public float Rotation
    {
        get
        {
            return _rotation;
        }

        set
        {
            _rotation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    public void ValidateLocation()
    {
        transform.localPosition = Location.Position;
    }

    public void Die()
    {
        Location.Unit = null;
        Destroy(gameObject);
    }
}
