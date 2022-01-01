using System;
using AarquieSolutions.DependencyInjection.ComponentField;
using UnityEngine;
using UnityEngine.EventSystems;
using AarquieSolutions.InspectorAttributes;

namespace AarquieSolutions.Utility
{
    public class DragAndDrop : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public enum TargetType
        {
            Tag,
            Object
        }
        
        [SerializeField] private TargetType targetType;

        [ShowIf("targetType", TargetType.Object)]
        [SerializeField] private RectTransform targetRectTransform;
        
        [ShowIf("targetType", TargetType.Tag)]
        [Tag]
        [SerializeField] private string targetTag;
        
        [SerializeField] private bool canMoveAgain;
        [SerializeField] private bool resetPositionIfTargetNotFound;
        
        [ShowIf("targetType", TargetType.Tag)]
        [SerializeField] private bool getTaggedGameobjectsEverytime;
        
        [GetComponent] private RectTransform rectTransform;
        private bool isOnTargetTransform;
        private Vector2 delta;
        private Vector3[] rectCorners;
        private RectTransform[] targetRectTransforms;
        private Vector3 initialPosition;

        //This event is fired when the draggable object is dropped on the target object, parameter1: draggable object, parameter2: target object, parameter3: initial position
        public Action<GameObject, GameObject, Vector3> DroppedOnTarget;
        
        protected void Start()
        {
            initialPosition = rectTransform.position;
            
            if (!getTaggedGameobjectsEverytime)
            {
                GetTargetObjectsWithTag();
            }
        }

        /// <summary>
        /// Get the world rect corners of the gameobject rect
        /// </summary>
        private void UpdateRectCorner()
        {
            rectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(rectCorners);
        }

        /// <summary>
        /// Check if the gameobject is overlapping any of the tagged gameobjects
        /// </summary>
        private void CheckIfOverlappingTaggedObjects()
        {
            UpdateTaggedRectTransform();
            
            foreach (RectTransform target in targetRectTransforms)
            {
                //Do not move back gameobject immediately since we all the tagged objects have to checked against the current position
                if (CheckIfInsideTarget(target, false))
                {
                    return;
                }
            }
            
            if (resetPositionIfTargetNotFound)
            {
                ResetPosition();
            }
        }

        /// <summary>
        /// Get all the gameobjects from the scene with the target tag
        /// </summary>
        private void GetTargetObjectsWithTag()
        {
            if (targetType == TargetType.Tag)
            {
                if (string.IsNullOrEmpty(targetTag))
                {
                    Debug.LogWarning("Tag is empty and drag and drop component has Target Type of Tag.");
                    return;
                }
            }

            UpdateTaggedRectTransform();
        }

        /// <summary>
        /// Update all the rectTransforms from scene with the target tag
        /// </summary>
        private void UpdateTaggedRectTransform()
        {
            GameObject[] targetObjects = GameObject.FindGameObjectsWithTag(targetTag);
            targetRectTransforms = new RectTransform[targetObjects.Length];
            for (int i = 0; i < targetObjects.Length; i++)
            {
                targetRectTransforms[i] = targetObjects[i].GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// Check if the draggable object is inside the target gameobject
        /// </summary>
        /// <param name="checkingRectTransform">The rectTransform to check against</param>
        /// <param name="moveBackImmediately">Should the draggable object move back immediately</param>
        /// <returns></returns>
        private bool CheckIfInsideTarget(RectTransform checkingRectTransform, bool moveBackImmediately = true)
        {
            UpdateRectCorner();
            
            Rect worldRect = new Rect(checkingRectTransform.position,
                new Vector2(checkingRectTransform.rect.width, checkingRectTransform.rect.height));

            for (int i = 0; i < 4; i++)
            {
                if (worldRect.Contains(rectCorners[i]))
                {
                    rectTransform.position = checkingRectTransform.position;
                    DroppedOnTarget?.Invoke(this.gameObject, checkingRectTransform.gameObject, initialPosition);
                    isOnTargetTransform = true;
                    return true;
                }
            }
            
            if (resetPositionIfTargetNotFound && moveBackImmediately)
            {
                ResetPosition();
            }

            return false;
        }

        /// <summary>
        /// Resets the positon of the draggable object
        /// </summary>
        public void ResetPosition()
        {
            isOnTargetTransform = false;
            rectTransform.position = initialPosition;
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!isOnTargetTransform || canMoveAgain)
            {
                rectTransform.position = (Vector2) Input.mousePosition + delta;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            delta = Input.mousePosition - rectTransform.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            switch (targetType)
            {
                case TargetType.Tag:
                    CheckIfOverlappingTaggedObjects();
                    break;
                case TargetType.Object:
                    CheckIfInsideTarget(targetRectTransform);
                    break;
                default:
                    break;
            }
        }
    }
}
