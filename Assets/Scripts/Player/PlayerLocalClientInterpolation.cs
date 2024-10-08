﻿using Fusion;
using UnityEngine;

namespace Player
{
    public class PlayerLocalClientInterpolation : NetworkBehaviour
    {
        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private Transform _interpolationTarget;

        private ChangeDetector _changeDetector;

        private float _interpolationTimer;

        private Vector3 _previousPosition;
        private Vector3 _currentPosition;

        private Quaternion _previousRotation;
        private Quaternion _currentRotation;

        [Networked]
        private Vector3 NetworkedPosition { get; set; }

        [Networked]
        private Quaternion NetworkedRotation { get; set; }

        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            if (HasStateAuthority)
            {
                CacheNetworkedOrientation();
            }
        }

        private void CacheNetworkedOrientation()
        {
            NetworkedPosition = _rigidbody.position;
            NetworkedRotation = _rigidbody.rotation;
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority)
            {
                CacheNetworkedOrientation();
            }
        }

        public override void Render()
        {
            base.Render();

            InterpolateLocalClient();
        }

        /// <summary>
        ///     Interpolation is very basic as I expected it to be a built-in feature,
        ///     but for some reason rigidbody is not synced with server if client has input authority
        ///     Some people mention it as a bug on Photon's discord and devs haven't responded to it so this is my simple
        ///     workaround
        /// </summary>
        private void InterpolateLocalClient()
        {
            if (HasInputAuthority && HasStateAuthority == false)
            {
                DetectNetworkedRigidBodyChanges();
                Interpolate();
            }
        }

        private void DetectNetworkedRigidBodyChanges()
        {
            var changedProperties = _changeDetector.DetectChanges(this, out var previousBuffer, out var currentBuffer);

            foreach (var propertyName in changedProperties)
            {
                switch (propertyName)
                {
                    case nameof(NetworkedPosition):
                    {
                        var positionReader = GetPropertyReader<Vector3>(propertyName);
                        (_previousPosition, _currentPosition) = positionReader.Read(previousBuffer, currentBuffer);
                        break;
                    }
                    case nameof(NetworkedRotation):
                        var rotationReader = GetPropertyReader<Quaternion>(propertyName);
                        (_previousRotation, _currentRotation) = rotationReader.Read(previousBuffer, currentBuffer);
                        break;
                }
            }
        }

        private void Interpolate()
        {
            _interpolationTimer += Time.deltaTime;
            var interpolationValue = Mathf.Clamp01(_interpolationTimer / Runner.DeltaTime);

            if (interpolationValue >= 1f)
            {
                _interpolationTimer = 0f;
                _previousPosition = _currentPosition;
                _previousRotation = _currentRotation;
            }

            _interpolationTarget.position = Vector3.Lerp(_previousPosition, _currentPosition, interpolationValue);
            _interpolationTarget.rotation = Quaternion.Slerp(_previousRotation, _currentRotation, interpolationValue);
        }
    }
}
