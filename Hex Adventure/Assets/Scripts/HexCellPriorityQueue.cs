using System.Collections.Generic;

public class HexCellPriorityQueue
{
    private int minimum = int.MaxValue;

    List<HexCell> list = new List<HexCell>();

    public void Enqueue(HexCell hexCell)
    {
        _count += 1;

        int priority = hexCell.SearchPriority;

        if(priority < minimum)
            minimum = priority;

        while (priority >= list.Count)
        {
            list.Add(null);
        }

        hexCell.NextWithSamePriority = list[priority]; // Cell reference
        list[priority] = hexCell;
    }

    public HexCell Dequeue()
    {
        _count -= 1;
        for(; minimum < list.Count; minimum++)
        {
            HexCell cell = list[minimum];

            if (cell != null)
            {
                list[minimum] = cell.NextWithSamePriority;
                return cell;
            }
        }
        return null;
    }

    public void Change(HexCell hexCell, int pastPriority)
    {
        HexCell current = list[pastPriority];
        HexCell next = current.NextWithSamePriority;
        
        if(current == hexCell)
        {
            list[pastPriority] = next;
        }
        else
        {
            while (next != hexCell)
            {
                current = next;
                next = current.NextWithSamePriority;
            }

            current.NextWithSamePriority = hexCell.NextWithSamePriority;
        }

        Enqueue(hexCell);
        _count -= 1;
    }

    public void Clear()
    {
        list.Clear();
        _count = 0;
        minimum = int.MaxValue;
    }

    int _count = 0;
    public int Count
    {
        get
        {
            return _count;
        }
    }
}
