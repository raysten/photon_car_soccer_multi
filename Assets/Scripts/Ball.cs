﻿using System;
using Fusion;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    public event Action OnEnteredGoal;
    
    private void OnTriggerStay(Collider otherCollider)
    {
        if (HasStateAuthority && otherCollider.gameObject.layer == Constants.GOAL_LAYER)
        {
            OnEnteredGoal?.Invoke();
        }
    }
}
