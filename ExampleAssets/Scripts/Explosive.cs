﻿using System;
using Thijs.Framework.MarchingSquares;
using UnityEngine;

public class Explosive : MonoBehaviour
{
    [SerializeField] private float radius = 1f;

    [Header("Collision")]
    [SerializeField] private float collisionOffset = 0f;

    private TileTerrain grid;

    private void OnEnable()
    {
        grid = FindObjectOfType<TileTerrain>();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        Vector2 center = transform.position;
        ContactPoint2D contact = other.contacts[0];
        center += contact.normal * radius * collisionOffset;

        grid.ModifyGrid(new GridModification()
        {
            ModifierShape = ModifierShape.Circle,
            setFilltype = FillType.None,
            position = center,
            size = radius,
        });

        //Debug.Break();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
