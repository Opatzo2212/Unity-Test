using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board
{
    public enum eMatchDirection
    {
        NONE,
        HORIZONTAL,
        VERTICAL,
        ALL
    }

    private int boardSizeX;

    private int boardSizeY;

    private List<Vector3> bottomBoardPos = new List<Vector3>();

    private Dictionary<Item, Cell> m_itemOriginCells = new Dictionary<Item, Cell>();

    private List<Item> bottomBoardItem = new List<Item>();

    private int bottomMax = 5;

    private Cell[,] m_cells;

    private Transform m_root;

    private int m_matchMin;

    public Board(Transform transform, GameSettings gameSettings)
    {
        m_root = transform;

        m_matchMin = gameSettings.MatchesMin;

        this.boardSizeX = gameSettings.BoardSizeX;
        this.boardSizeY = gameSettings.BoardSizeY;

        m_cells = new Cell[boardSizeX, boardSizeY];

        CreateBoard();
        CreateBottomBoard();
    }

    private void CreateBoard()
    {
        Vector3 origin = new Vector3(-boardSizeX * 0.5f + 0.5f, -boardSizeY * 0.5f + 0.5f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                GameObject go = GameObject.Instantiate(prefabBG);
                go.transform.position = origin + new Vector3(x, y, 0f);
                go.transform.SetParent(m_root);

                Cell cell = go.GetComponent<Cell>();
                cell.Setup(x, y);

                m_cells[x, y] = cell;
            }
        }

        //set neighbours
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                if (y + 1 < boardSizeY) m_cells[x, y].NeighbourUp = m_cells[x, y + 1];
                if (x + 1 < boardSizeX) m_cells[x, y].NeighbourRight = m_cells[x + 1, y];
                if (y > 0) m_cells[x, y].NeighbourBottom = m_cells[x, y - 1];
                if (x > 0) m_cells[x, y].NeighbourLeft = m_cells[x - 1, y];
            }
        }

    }

    private void CreateBottomBoard()
    {
        float startX = -2f;
        float y = -4f;

        for (int i = 0; i < bottomMax; i++)
        {
            Vector3 pos = new Vector3(startX + i, y, 0f);

            bottomBoardPos.Add(pos);

            GameObject slot = GameObject.Instantiate(Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND));

            slot.transform.position = pos;

            slot.transform.SetParent(m_root);

            slot.transform.localScale = Vector3.one * 0.8f;
        }
    }

    internal void Fill()
    {
        List<NormalItem.eNormalType> allItems =
            new List<NormalItem.eNormalType>();

        NormalItem.eNormalType[] allTypes =
            (NormalItem.eNormalType[])Enum.GetValues(
                typeof(NormalItem.eNormalType));

        int totalCells = boardSizeX * boardSizeY;

        totalCells -= totalCells % 3;

        foreach (var type in allTypes)
        {
            allItems.Add(type);
            allItems.Add(type);
            allItems.Add(type);
        }

        while (allItems.Count < totalCells)
        {
            var randomType =
                allTypes[UnityEngine.Random.Range(0, allTypes.Length)];

            allItems.Add(randomType);
            allItems.Add(randomType);
            allItems.Add(randomType);
        }

        allItems = allItems.OrderBy(x => UnityEngine.Random.value).ToList();

        int index = 0;

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                if (index >= allItems.Count)
                    return;

                Cell cell = m_cells[x, y];

                NormalItem item = new NormalItem();

                item.SetType(allItems[index]);

                item.SetView();

                item.SetViewRoot(m_root);

                cell.Assign(item);

                cell.ApplyItemPosition(false);

                index++;
            }
        }
    }

    public void AddToBottom(Cell cell)
    {
        if (bottomBoardItem.Count >= bottomMax)
        {
            return;
        }

        if (cell == null || cell.IsEmpty)
            return;

        Item item = cell.Item;

        m_itemOriginCells[item] = cell;

        cell.Free();

        NormalItem normalItem = item as NormalItem;
        int insertIndex = bottomBoardItem.Count; 

        if (normalItem != null)
        {
            for (int i = bottomBoardItem.Count - 1; i >= 0; i--)
            {
                NormalItem bottomItem = bottomBoardItem[i] as NormalItem;
                if (bottomItem != null && bottomItem.ItemType == normalItem.ItemType)
                {
                    insertIndex = i + 1; 
                    break;
                }
            }
        }

        bottomBoardItem.Insert(insertIndex, item);

        RearrangeBottom();

        CheckBottomMatches();
    }

    private void CheckBottomMatches()
    {
        Dictionary<NormalItem.eNormalType, List<Item>> groups =
            new Dictionary<NormalItem.eNormalType, List<Item>>();

        foreach (Item item in bottomBoardItem)
        {
            NormalItem normal = item as NormalItem;

            if (normal == null)
                continue;

            if (!groups.ContainsKey(normal.ItemType))
            {
                groups[normal.ItemType] = new List<Item>();
            }

            groups[normal.ItemType].Add(item);
        }

        foreach (var pair in groups)
        {
            if (pair.Value.Count >= 3)
            {
                RemoveBottomItems(pair.Value.GetRange(0, 3));
                return;
            }
        }
    }

    private void RemoveBottomItems(List<Item> itemsToRemove)
    {
        foreach (Item item in itemsToRemove)
        {
            bottomBoardItem.Remove(item);
            m_itemOriginCells.Remove(item);

            item.View.DOScale(Vector3.zero, 0.15f).SetDelay(0.15f);
        }

        DOVirtual.DelayedCall(0.25f, () =>
        {
            RearrangeBottom();
        });

        DOVirtual.DelayedCall(0.4f, () =>
        {
            foreach (Item item in itemsToRemove)
            {
                if (item != null && item.View != null && item.View.gameObject != null)
                {
                    GameObject.Destroy(item.View.gameObject);
                }
            }
        });
    }

    private void RearrangeBottom()
    {
        for (int i = 0; i < bottomBoardItem.Count; i++)
        {
            bottomBoardItem[i].View.DOMove(bottomBoardPos[i], 0.2f);
        }
    }

    public bool IsBoardEmpty()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                if (m_cells[x, y] != null && !m_cells[x, y].IsEmpty)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public bool IsBottomFull()
    {
        return bottomBoardItem.Count >= bottomMax;
    }

    public List<Item> GetBottomBoardItems()
    {
        return bottomBoardItem;
    }

    public void ReturnFromBottom(Item item)
    {
        if (!bottomBoardItem.Contains(item)) return;

        Cell originCell = m_itemOriginCells.ContainsKey(item) ? m_itemOriginCells[item] : null;

        if (originCell != null && originCell.IsEmpty)
        {
            bottomBoardItem.Remove(item);
            m_itemOriginCells.Remove(item);

            originCell.Assign(item);
            item.View.DOMove(originCell.transform.position, 0.3f);

            RearrangeBottom();
        }
    }

    //internal void Shuffle()
    //{
    //    List<Item> list = new List<Item>();
    //    for (int x = 0; x < boardSizeX; x++)
    //    {
    //        for (int y = 0; y < boardSizeY; y++)
    //        {
    //            list.Add(m_cells[x, y].Item);
    //            m_cells[x, y].Free();
    //        }
    //    }

    //    for (int x = 0; x < boardSizeX; x++)
    //    {
    //        for (int y = 0; y < boardSizeY; y++)
    //        {
    //            int rnd = UnityEngine.Random.Range(0, list.Count);
    //            m_cells[x, y].Assign(list[rnd]);
    //            m_cells[x, y].ApplyItemMoveToPosition();

    //            list.RemoveAt(rnd);
    //        }
    //    }
    //}


    //internal void FillGapsWithNewItems()
    //{
    //    for (int x = 0; x < boardSizeX; x++)
    //    {
    //        for (int y = 0; y < boardSizeY; y++)
    //        {
    //            Cell cell = m_cells[x, y];
    //            if (!cell.IsEmpty) continue;

    //            NormalItem item = new NormalItem();

    //            item.SetType(Utils.GetRandomNormalType());
    //            item.SetView();
    //            item.SetViewRoot(m_root);

    //            cell.Assign(item);
    //            cell.ApplyItemPosition(true);
    //        }
    //    }
    //}

    //internal void ExplodeAllItems()
    //{
    //    for (int x = 0; x < boardSizeX; x++)
    //    {
    //        for (int y = 0; y < boardSizeY; y++)
    //        {
    //            Cell cell = m_cells[x, y];
    //            cell.ExplodeItem();
    //        }
    //    }
    //}

    //public void Swap(Cell cell1, Cell cell2, Action callback)
    //{
    //    Item item = cell1.Item;
    //    cell1.Free();
    //    Item item2 = cell2.Item;
    //    cell1.Assign(item2);
    //    cell2.Free();
    //    cell2.Assign(item);

    //    item.View.DOMove(cell2.transform.position, 0.3f);
    //    item2.View.DOMove(cell1.transform.position, 0.3f).OnComplete(() => { if (callback != null) callback(); });
    //}

    public List<Cell> GetHorizontalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell>();
        list.Add(cell);

        //check horizontal match
        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourRight;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourLeft;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return list;
    }


    public List<Cell> GetVerticalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell>();
        list.Add(cell);

        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourUp;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourBottom;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return list;
    }

    //internal void ConvertNormalToBonus(List<Cell> matches, Cell cellToConvert)
    //{
    //    eMatchDirection dir = GetMatchDirection(matches);

    //    BonusItem item = new BonusItem();
    //    switch (dir)
    //    {
    //        case eMatchDirection.ALL:
    //            item.SetType(BonusItem.eBonusType.ALL);
    //            break;
    //        case eMatchDirection.HORIZONTAL:
    //            item.SetType(BonusItem.eBonusType.HORIZONTAL);
    //            break;
    //        case eMatchDirection.VERTICAL:
    //            item.SetType(BonusItem.eBonusType.VERTICAL);
    //            break;
    //    }

    //    if (item != null)
    //    {
    //        if (cellToConvert == null)
    //        {
    //            int rnd = UnityEngine.Random.Range(0, matches.Count);
    //            cellToConvert = matches[rnd];
    //        }

    //        item.SetView();
    //        item.SetViewRoot(m_root);

    //        cellToConvert.Free();
    //        cellToConvert.Assign(item);
    //        cellToConvert.ApplyItemPosition(true);
    //    }
    //}


    internal eMatchDirection GetMatchDirection(List<Cell> matches)
    {
        if (matches == null || matches.Count < m_matchMin) return eMatchDirection.NONE;

        var listH = matches.Where(x => x.BoardX == matches[0].BoardX).ToList();
        if (listH.Count == matches.Count)
        {
            return eMatchDirection.VERTICAL;
        }

        var listV = matches.Where(x => x.BoardY == matches[0].BoardY).ToList();
        if (listV.Count == matches.Count)
        {
            return eMatchDirection.HORIZONTAL;
        }

        if (matches.Count > 5)
        {
            return eMatchDirection.ALL;
        }

        return eMatchDirection.NONE;
    }

    internal List<Cell> FindFirstMatch()
    {
        List<Cell> list = new List<Cell>();

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                var listhor = GetHorizontalMatches(cell);
                if (listhor.Count >= m_matchMin)
                {
                    list = listhor;
                    break;
                }

                var listvert = GetVerticalMatches(cell);
                if (listvert.Count >= m_matchMin)
                {
                    list = listvert;
                    break;
                }
            }
        }

        return list;
    }

    public List<Cell> CheckBonusIfCompatible(List<Cell> matches)
    {
        var dir = GetMatchDirection(matches);

        var bonus = matches.Where(x => x.Item is BonusItem).FirstOrDefault();
        if(bonus == null)
        {
            return matches;
        }

        List<Cell> result = new List<Cell>();
        switch (dir)
        {
            case eMatchDirection.HORIZONTAL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.HORIZONTAL)
                    {
                        result.Add(cell);
                    }
                }
                break;
            case eMatchDirection.VERTICAL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.VERTICAL)
                    {
                        result.Add(cell);
                    }
                }
                break;
            case eMatchDirection.ALL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.ALL)
                    {
                        result.Add(cell);
                    }
                }
                break;
        }

        return result;
    }

    //internal List<Cell> GetPotentialMatches()
    //{
    //    List<Cell> result = new List<Cell>();
    //    for (int x = 0; x < boardSizeX; x++)
    //    {
    //        for (int y = 0; y < boardSizeY; y++)
    //        {
    //            Cell cell = m_cells[x, y];

    //            //check right
    //            /* example *\
    //              * * * * *
    //              * * * * *
    //              * * * ? *
    //              * & & * ?
    //              * * * ? *
    //            \* example  */

    //            if (cell.NeighbourRight != null)
    //            {
    //                result = GetPotentialMatch(cell, cell.NeighbourRight, cell.NeighbourRight.NeighbourRight);
    //                if (result.Count > 0)
    //                {
    //                    break;
    //                }
    //            }

    //            //check up
    //            /* example *\
    //              * ? * * *
    //              ? * ? * *
    //              * & * * *
    //              * & * * *
    //              * * * * *
    //            \* example  */
    //            if (cell.NeighbourUp != null)
    //            {
    //                result = GetPotentialMatch(cell, cell.NeighbourUp, cell.NeighbourUp.NeighbourUp);
    //                if (result.Count > 0)
    //                {
    //                    break;
    //                }
    //            }

    //            //check bottom
    //            /* example *\
    //              * * * * *
    //              * & * * *
    //              * & * * *
    //              ? * ? * *
    //              * ? * * *
    //            \* example  */
    //            if (cell.NeighbourBottom != null)
    //            {
    //                result = GetPotentialMatch(cell, cell.NeighbourBottom, cell.NeighbourBottom.NeighbourBottom);
    //                if (result.Count > 0)
    //                {
    //                    break;
    //                }
    //            }

    //            //check left
    //            /* example *\
    //              * * * * *
    //              * * * * *
    //              * ? * * *
    //              ? * & & *
    //              * ? * * *
    //            \* example  */
    //            if (cell.NeighbourLeft != null)
    //            {
    //                result = GetPotentialMatch(cell, cell.NeighbourLeft, cell.NeighbourLeft.NeighbourLeft);
    //                if (result.Count > 0)
    //                {
    //                    break;
    //                }
    //            }

    //            /* example *\
    //              * * * * *
    //              * * * * *
    //              * * ? * *
    //              * & * & *
    //              * * ? * *
    //            \* example  */
    //            Cell neib = cell.NeighbourRight;
    //            if (neib != null && neib.NeighbourRight != null && neib.NeighbourRight.IsSameType(cell))
    //            {
    //                Cell second = LookForTheSecondCellVertical(neib, cell);
    //                if (second != null)
    //                {
    //                    result.Add(cell);
    //                    result.Add(neib.NeighbourRight);
    //                    result.Add(second);
    //                    break;
    //                }
    //            }

    //            /* example *\
    //              * * * * *
    //              * & * * *
    //              ? * ? * *
    //              * & * * *
    //              * * * * *
    //            \* example  */
    //            neib = null;
    //            neib = cell.NeighbourUp;
    //            if (neib != null && neib.NeighbourUp != null && neib.NeighbourUp.IsSameType(cell))
    //            {
    //                Cell second = LookForTheSecondCellHorizontal(neib, cell);
    //                if (second != null)
    //                {
    //                    result.Add(cell);
    //                    result.Add(neib.NeighbourUp);
    //                    result.Add(second);
    //                    break;
    //                }
    //            }
    //        }

    //        if (result.Count > 0) break;
    //    }

    //    return result;
    //}

    private List<Cell> GetPotentialMatch(Cell cell, Cell neighbour, Cell target)
    {
        List<Cell> result = new List<Cell>();

        if (neighbour != null && neighbour.IsSameType(cell))
        {
            Cell third = LookForTheThirdCell(target, neighbour);
            if (third != null)
            {
                result.Add(cell);
                result.Add(neighbour);
                result.Add(third);
            }
        }

        return result;
    }

    private Cell LookForTheSecondCellHorizontal(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look right
        Cell second = null;
        second = target.NeighbourRight;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        //look left
        second = null;
        second = target.NeighbourLeft;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheSecondCellVertical(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look up        
        Cell second = target.NeighbourUp;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        //look bottom
        second = null;
        second = target.NeighbourBottom;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheThirdCell(Cell target, Cell main)
    {
        if (target == null) return null;
        if (target.IsSameType(main)) return null;

        //look up
        Cell third = CheckThirdCell(target.NeighbourUp, main);
        if (third != null)
        {
            return third;
        }

        //look right
        third = null;
        third = CheckThirdCell(target.NeighbourRight, main);
        if (third != null)
        {
            return third;
        }

        //look bottom
        third = null;
        third = CheckThirdCell(target.NeighbourBottom, main);
        if (third != null)
        {
            return third;
        }

        //look left
        third = null;
        third = CheckThirdCell(target.NeighbourLeft, main); ;
        if (third != null)
        {
            return third;
        }

        return null;
    }

    private Cell CheckThirdCell(Cell target, Cell main)
    {
        if (target != null && target != main && target.IsSameType(main))
        {
            return target;
        }

        return null;
    }

    //internal void ShiftDownItems()
    //{
    //    for (int x = 0; x < boardSizeX; x++)
    //    {
    //        int shifts = 0;
    //        for (int y = 0; y < boardSizeY; y++)
    //        {
    //            Cell cell = m_cells[x, y];
    //            if (cell.IsEmpty)
    //            {
    //                shifts++;
    //                continue;
    //            }

    //            if (shifts == 0) continue;

    //            Cell holder = m_cells[x, y - shifts];

    //            Item item = cell.Item;
    //            cell.Free();

    //            holder.Assign(item);
    //            item.View.DOMove(holder.transform.position, 0.3f);
    //        }
    //    }
    //}

    public Cell GetHintForWin()
    {
        List<NormalItem.eNormalType> bottomTypes = new List<NormalItem.eNormalType>();
        foreach (var item in bottomBoardItem)
        {
            if (item is NormalItem normal && !bottomTypes.Contains(normal.ItemType))
            {
                bottomTypes.Add(normal.ItemType);
            }
        }

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (cell != null && !cell.IsEmpty && cell.Item is NormalItem normal)
                {
                    if (bottomTypes.Contains(normal.ItemType)) return cell;
                }
            }
        }

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (cell != null && !cell.IsEmpty) return cell;
            }
        }
        return null;
    }

    public Cell GetHintForLose()
    {
        List<NormalItem.eNormalType> bottomTypes = new List<NormalItem.eNormalType>();
        foreach (var item in bottomBoardItem)
        {
            if (item is NormalItem normal && !bottomTypes.Contains(normal.ItemType))
            {
                bottomTypes.Add(normal.ItemType);
            }
        }

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (cell != null && !cell.IsEmpty && cell.Item is NormalItem normal)
                {
                    if (!bottomTypes.Contains(normal.ItemType)) return cell;
                }
            }
        }

        return GetHintForWin();
    }

    public void Clear()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.Clear();

                GameObject.Destroy(cell.gameObject);
                m_cells[x, y] = null;
            }
        }
    }
}
