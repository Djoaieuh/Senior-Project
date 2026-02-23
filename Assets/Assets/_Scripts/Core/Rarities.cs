using System.Collections.Generic;
using UnityEngine;

public enum BaitType
{
    None,
    Sweet,
    Savory,
    Worm,
    Lure
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Extraordinary,
    Mythical
}

public enum ItemType
{
    Fish,
    Plant,
    Metal,
    Magical,
    Other,
    Quest,
    Consumable,
    Null
}

public enum FishWeightClass
{
    Minuscule,
    Tiny,
    Small,
    Modest,
    Stout,
    Large,
    Great,
    Grand,
    Massive,
    Colossal,
    Titanic
}

public enum ReelButton
{
    Button1,  // Keyboard 1 / Controller A
    Button2,  // Keyboard 2 / Controller B  
    Button3,  // Keyboard 3 / Controller X
    Button4   // Keyboard 4 / Controller Y
}

[System.Serializable]
public class ReelSequence
{
    [SerializeField] private List<ReelButton> sequence = new List<ReelButton>();
    
    private int currentIndex = 0;
    
    public ReelSequence(List<ReelButton> initialSequence)
    {
        sequence = new List<ReelButton>(initialSequence);
    }
    
    /// <summary>
    /// Get the next N buttons in the sequence (always wraps around)
    /// </summary>
    public List<ReelButton> GetNext(int count)
    {
        List<ReelButton> result = new List<ReelButton>();
        
        if (sequence.Count == 0) return result;
        
        for (int i = 0; i < count; i++)
        {
            int index = (currentIndex + i) % sequence.Count;
            result.Add(sequence[index]);
        }
        
        return result;
    }
    
    /// <summary>
    /// Get the current button without advancing
    /// </summary>
    public ReelButton GetCurrent()
    {
        if (sequence.Count == 0)
        {
            Debug.LogWarning("Sequence is empty!");
            return ReelButton.Button1;
        }
        
        return sequence[currentIndex % sequence.Count];
    }
    
    /// <summary>
    /// Advance to the next button in the sequence
    /// </summary>
    public void Advance()
    {
        if (sequence.Count == 0) return;
        currentIndex = (currentIndex + 1) % sequence.Count;
    }
    
    /// <summary>
    /// Advance by N steps
    /// </summary>
    public void Advance(int steps)
    {
        if (sequence.Count == 0) return;
        currentIndex = (currentIndex + steps) % sequence.Count;
    }
    
    /// <summary>
    /// Reset the sequence back to the beginning
    /// </summary>
    public void Reset()
    {
        currentIndex = 0;
    }
    
    public int Length => sequence.Count;
    public int CurrentIndex => currentIndex;
    public bool IsEmpty => sequence.Count == 0;
}