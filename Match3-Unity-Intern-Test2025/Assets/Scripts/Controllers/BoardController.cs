using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;

    private bool m_isDragging;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;

    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private float m_timeAttackTimer = 60f;

    private Text m_uiTimerText;

    private bool m_hintIsShown;

    private bool m_gameOver;

    public void StartGame(GameManager gameManager, GameSettings gameSettings, Text uiText = null)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;

        m_uiTimerText = uiText;

        m_timeAttackTimer = 60f;

        if (m_gameManager.CurrentMode == GameManager.eLevelMode.TIMER && m_uiTimerText != null)
        {
            m_uiTimerText.text = "TIME:\n" + Mathf.CeilToInt(m_timeAttackTimer).ToString();
        }

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        Fill();

        if (m_gameManager.AutoPlayMode != GameManager.eAutoPlayMode.NONE)
        {
            StartCoroutine(AutoPlayRoutine());
        }
    }

    private void Fill()
    {
        m_board.Fill();
        //FindMatchesAndCollapse();
    }

    private IEnumerator AutoPlayRoutine()
    {
        yield return new WaitForSeconds(0.5f); 

        while (!m_gameOver)
        {
            if (IsBusy)
            {
                yield return null;
                continue;
            }

            Cell targetCell = null;
            if (m_gameManager.AutoPlayMode == GameManager.eAutoPlayMode.WIN)
            {
                targetCell = m_board.GetHintForWin();
            }
            else if (m_gameManager.AutoPlayMode == GameManager.eAutoPlayMode.LOSE)
            {
                targetCell = m_board.GetHintForLose();
            }

            if (targetCell != null)
            {
                ProcessCellClick(targetCell);
            }

            yield return new WaitForSeconds(0.5f); 
        }
    }

    private void ProcessCellClick(Cell cell)
    {
        if (cell != null && !cell.IsEmpty)
        {
            if (m_gameManager.CurrentMode == GameManager.eLevelMode.TIMER && m_board.IsBottomFull())
            {
                return;
            }

            m_board.AddToBottom(cell);

            if (m_board.IsBoardEmpty())
            {
                m_gameOver = true;
                m_gameManager.GameWin();
            }
            else if (m_board.IsBottomFull() && m_gameManager.CurrentMode != GameManager.eLevelMode.TIMER)
            {
                m_gameOver = true;
                m_gameManager.GameOver();
            }
        }
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                //StopHints();
                break;
        }
    }


    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;

        //if (!m_hintIsShown)
        //{
        //    m_timeAfterFill += Time.deltaTime;
        //    if (m_timeAfterFill > m_gameSettings.TimeForHint)
        //    {
        //        m_timeAfterFill = 0f;
        //        //ShowHint();
        //    }
        //}
        if (m_gameManager.CurrentMode == GameManager.eLevelMode.TIMER)
        {
            m_timeAttackTimer -= Time.deltaTime;
            if (m_uiTimerText != null)
            {
                m_uiTimerText.text = "TIME:\n" + Mathf.CeilToInt(m_timeAttackTimer).ToString();
            }

            if (m_timeAttackTimer <= 0)
            {
                m_gameOver = true;
                m_gameManager.GameOver();
                return;
            }
        }
        if (m_gameManager.AutoPlayMode == GameManager.eAutoPlayMode.NONE && Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = m_cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;

            if (m_gameManager.CurrentMode == GameManager.eLevelMode.TIMER)
            {
                Item clickedBottomItem = null;
                foreach (var item in m_board.GetBottomBoardItems())
                {
                    if (item.View != null && Vector3.Distance(mousePos, item.View.position) < 0.8f)
                    {
                        clickedBottomItem = item;
                        break;
                    }
                }

                if (clickedBottomItem != null)
                {
                    m_board.ReturnFromBottom(clickedBottomItem);
                    return;
                }
            }

            var hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null)
            {
                ProcessCellClick(hit.collider.GetComponent<Cell>());
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                Cell cell = hit.collider.GetComponent<Cell>();

                if (cell != null && !cell.IsEmpty)
                {
                    m_board.AddToBottom(cell);

                    if (m_board.IsBoardEmpty())
                    {
                        m_gameOver = true;
                        m_gameManager.GameWin();
                    }
                    else if (m_board.IsBottomFull())
                    {
                        m_gameOver = true;
                        m_gameManager.GameOver();
                    }
                }
            }
        }

        //if (Input.GetMouseButtonUp(0))
        //{
        //    ResetRayCast();
        //}

        //if (Input.GetMouseButton(0) && m_isDragging)
        //{
        //    var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        //    if (hit.collider != null)
        //    {
        //        if (m_hitCollider != null && m_hitCollider != hit.collider)
        //        {
        //            //StopHints();

        //            Cell c1 = m_hitCollider.GetComponent<Cell>();
        //            Cell c2 = hit.collider.GetComponent<Cell>();
        //            //if (AreItemsNeighbor(c1, c2))
        //            //{
        //            //    IsBusy = true;
        //            //    SetSortingLayer(c1, c2);
        //            //    m_board.Swap(c1, c2, () =>
        //            //    {
        //            //        FindMatchesAndCollapse(c1, c2);
        //            //    });

        //            //    ResetRayCast();
        //            //}
        //        }
        //    }
        //    else
        //    {
        //        ResetRayCast();
        //    }
        //}
    }

    private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    //private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    //{
    //    if (cell1.Item is BonusItem)
    //    {
    //        cell1.ExplodeItem();
    //        StartCoroutine(ShiftDownItemsCoroutine());
    //    }
    //    else if (cell2.Item is BonusItem)
    //    {
    //        cell2.ExplodeItem();
    //        StartCoroutine(ShiftDownItemsCoroutine());
    //    }
    //    else
    //    {
    //        List<Cell> cells1 = GetMatches(cell1);
    //        List<Cell> cells2 = GetMatches(cell2);

    //        List<Cell> matches = new List<Cell>();
    //        matches.AddRange(cells1);
    //        matches.AddRange(cells2);
    //        matches = matches.Distinct().ToList();

    //        if (matches.Count < m_gameSettings.MatchesMin)
    //        {
    //            m_board.Swap(cell1, cell2, () =>
    //            {
    //                IsBusy = false;
    //            });
    //        }
    //        else
    //        {
    //            OnMoveEvent();

    //            CollapseMatches(matches, cell2);
    //        }
    //    }
    //}

    //private void FindMatchesAndCollapse()
    //{
    //    List<Cell> matches = m_board.FindFirstMatch();

    //    if (matches.Count > 0)
    //    {
    //        CollapseMatches(matches, null);
    //    }
    //    else
    //    {
    //        m_potentialMatch = m_board.GetPotentialMatches();
    //        if (m_potentialMatch.Count > 0)
    //        {
    //            IsBusy = false;

    //            m_timeAfterFill = 0f;
    //        }
    //        else
    //        {
    //            //StartCoroutine(RefillBoardCoroutine());
    //            StartCoroutine(ShuffleBoardCoroutine());
    //        }
    //    }
    //}

    //private List<Cell> GetMatches(Cell cell)
    //{
    //    List<Cell> listHor = m_board.GetHorizontalMatches(cell);
    //    if (listHor.Count < m_gameSettings.MatchesMin)
    //    {
    //        listHor.Clear();
    //    }

    //    List<Cell> listVert = m_board.GetVerticalMatches(cell);
    //    if (listVert.Count < m_gameSettings.MatchesMin)
    //    {
    //        listVert.Clear();
    //    }

    //    return listHor.Concat(listVert).Distinct().ToList();
    //}

    //private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    //{
    //    for (int i = 0; i < matches.Count; i++)
    //    {
    //        matches[i].ExplodeItem();
    //    }

    //    if(matches.Count > m_gameSettings.MatchesMin)
    //    {
    //        m_board.ConvertNormalToBonus(matches, cellEnd);
    //    }

    //    StartCoroutine(ShiftDownItemsCoroutine());
    //}

    //private IEnumerator ShiftDownItemsCoroutine()
    //{
    //    m_board.ShiftDownItems();

    //    yield return new WaitForSeconds(0.2f);

    //    m_board.FillGapsWithNewItems();

    //    yield return new WaitForSeconds(0.2f);

    //    FindMatchesAndCollapse();
    //}

    //private IEnumerator RefillBoardCoroutine()
    //{
    //    m_board.ExplodeAllItems();

    //    yield return new WaitForSeconds(0.2f);

    //    m_board.Fill();

    //    yield return new WaitForSeconds(0.2f);

    //    FindMatchesAndCollapse();
    //}

    //private IEnumerator ShuffleBoardCoroutine()
    //{
    //    m_board.Shuffle();

    //    yield return new WaitForSeconds(0.3f);

    //    FindMatchesAndCollapse();
    //}


    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    //private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    //{
    //    return cell1.IsNeighbour(cell2);
    //}

    internal void Clear()
    {
        m_board.Clear();
    }

    //private void ShowHint()
    //{
    //    m_hintIsShown = true;
    //    foreach (var cell in m_potentialMatch)
    //    {
    //        cell.AnimateItemForHint();
    //    }
    //}

    //private void StopHints()
    //{
    //    m_hintIsShown = false;
    //    foreach (var cell in m_potentialMatch)
    //    {
    //        cell.StopHintAnimation();
    //    }

    //    m_potentialMatch.Clear();
    //}
}
