﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GreeningEx2019
{
    [CreateAssetMenu(menuName = "Greening/Stella Actions/Create Nae Walk", fileName = "StellaActionNaeWalk")]
    public class StellaNaeWalk : StellaWalk
    {
        [Tooltip("苗を置ける場所の単位距離"), SerializeField]
        float naeUnit = 1f;
        [Tooltip("苗を置ける下方向の高さ"), SerializeField]
        float naePutHeight = 0.5f;

        const string GroundTag = "Ground";
        const int HitMax = 8;

        NaeActable naeActable = null;
        int groundLayer;
        int overlapLayer;
        RaycastHit[] hits = new RaycastHit[HitMax];

        public override void Init()
        {
            base.Init();

            naeActable = (NaeActable)ActionBox.SelectedActable;
            groundLayer = LayerMask.GetMask("MapCollision");
            overlapLayer = LayerMask.GetMask("MapCollision", "Nae", "MapTrigger");
        }

        /// <summary>
        /// 指定の座標に苗が置けるかを確認して、フラグで返します。
        /// </summary>
        /// <param name="pos">苗を置きたい地面の座標</param>
        /// <returns>true=置ける / false=置けない</returns>
        bool CheckPut(Vector3 pos)
        {
            // 置く先が、ステラの足元より一定以上低い場合は置けない
            if (pos.y < (StellaMove.chrController.bounds.min.y-naePutHeight)) return false;

            // 重なっているオブジェクトを探査
            int hitCount = naeActable.FetchOverlapObjects(pos, hits, overlapLayer);
            return (hitCount == 0);
        }

        public override void UpdateAction()
        {
            // ターン中処理
            if (state == StateType.Turn)
            {
                Turn();
                return;
            }

            // 置く候補の地面の座標
            Vector3 naepos = GetPutPosition(StellaMove.instance.transform.position);
            bool canPut = CheckPut(naepos);

            if (canPut)
            {
                // 置けるならボタンによって苗を置く
                if (Input.GetButton("Water") || Input.GetButton("Action"))
                {
                    StellaMove.instance.ChangeAction(StellaMove.ActionType.Putdown);
                    return;
                }

                // 今置いたらここという場所に苗マーカーを表示
                NaeActable.MarkerObject.SetActive(true);
                naepos.y = StellaMove.chrController.bounds.min.y + naeActable.HeightFromGround;
                NaeActable.MarkerObject.transform.position = naepos;
            }
            else
            {
                // 置けない時はマーカーを非表示
                NaeActable.MarkerObject.SetActive(false);
            }

            // 行動
            Walk();
            StellaMove.instance.Gravity();
            StellaMove.instance.Move();

            if (!StellaMove.chrController.isGrounded)
            {
                StellaMove.instance.ChangeAction(StellaMove.ActionType.Air);
                FallNextBlock();
            }
            else
            {
                StellaMove.instance.CheckMiniJump();
            }
        }

        /// <summary>
        /// 苗を置く候補座標を返します。
        /// </summary>
        /// <param name="stellaPosition">ステラの座標</param>
        /// <returns>求めた苗の座標</returns>
        Vector3 GetPutPosition(Vector3 stellaPosition)
        {
            Vector3 naepos = stellaPosition;
            float absOffset = StellaMove.ActionBoxInstance.colliderCenter.x
                + StellaMove.ActionBoxInstance.halfExtents.x
                + naeActable.ColliderExtentsX;
            float baseX = naepos.x + absOffset * StellaMove.forwardVector.x;

            // 単位変換
            naepos.x = Mathf.Round(baseX / naeUnit) * naeUnit;
            if (absOffset < (Mathf.Abs(naepos.x - stellaPosition.x)))
            {
                // 遠くなっているので、1単位近づける
                naepos.x -= naeUnit * StellaMove.forwardVector.x;
            }

            // 床の位置を調べる
            naepos.y = StellaMove.chrController.bounds.min.y;
            int hitCount = Physics.RaycastNonAlloc(naepos, Vector3.down, hits, float.PositiveInfinity, groundLayer);
            if (hitCount ==0)
            {
                // 置けない高さを設定
                naepos.y -= naePutHeight * 2f;
                return naepos;
            }

            naepos.y = hits[0].collider.bounds.max.y;
            for (int i = 1; i < hitCount; i++)
            {
                if (hits[i].collider.CompareTag(GroundTag))
                {
                    if (hits[i].collider.bounds.max.y > naepos.y)
                    {
                        naepos.y = hits[i].collider.bounds.max.y;
                    }
                }
            }

            return naepos;
        }
    }
}