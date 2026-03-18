using System;
using UnityEngine;

namespace Scripts
{
    public class ClimbData : MonoBehaviour
    {
        public Transform target;
        public Transform startClimb;
        public Transform finishClimb;
        public float rangeClimb = 0.5f;
        
        public bool isActive = true;

        public Vector3 GetPointStartClimb(Transform player)
        {
            var localPlayerPoint = startClimb.InverseTransformPoint(player.position);

            var x = Math.Clamp(localPlayerPoint.x, startClimb.position.x - rangeClimb,
                startClimb.position.x + rangeClimb);
            
            return new Vector3(x, startClimb.position.y, startClimb.position.z);
        }
        
        public Vector3 GetPointFinishClimb(Transform player)
        {
            var localPlayerPoint = finishClimb.InverseTransformPoint(player.position);

            var x = Math.Clamp(localPlayerPoint.x, finishClimb.position.x - rangeClimb,
                finishClimb.position.x + rangeClimb);
            
            return new Vector3(x, finishClimb.position.y, finishClimb.position.z);
        }
        
        private void OnDrawGizmos()
        {
            if (startClimb == null || finishClimb == null) return;

            Gizmos.color = Color.green;
    
            Vector3 leftStart  = startClimb.TransformPoint(new Vector3(-rangeClimb, 0f, 0f));
            Vector3 rightStart = startClimb.TransformPoint(new Vector3( rangeClimb, 0f, 0f));
    
            Gizmos.DrawSphere(leftStart,  0.1f);
            Gizmos.DrawSphere(rightStart, 0.1f);
            Gizmos.DrawLine(leftStart, rightStart);
    
            Gizmos.color = Color.blue;
    
            Vector3 leftFinish  = finishClimb.TransformPoint(new Vector3(-rangeClimb, 0f, 0f));
            Vector3 rightFinish = finishClimb.TransformPoint(new Vector3( rangeClimb, 0f, 0f));
    
            Gizmos.DrawSphere(leftFinish,  0.1f);
            Gizmos.DrawSphere(rightFinish, 0.1f);
            Gizmos.DrawLine(leftFinish, rightFinish);
        }
    }
}