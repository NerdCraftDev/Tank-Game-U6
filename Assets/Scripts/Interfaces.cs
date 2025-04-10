using UnityEngine;

public interface IShootable
{
    void OnHit(Bullet bulletComponent, Vector3 hitPoint, Vector3 hitNormal, GameObject shooter);
}
