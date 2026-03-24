using UnityEngine;
using System.Collections.Generic;

public class HRManager : MonoBehaviour
{
    public static HRManager Instance;

    [Header("HR database")]
    public List<Employee> unassignedEmployees = new List<Employee>(); 
    public List<Employee> marketCandidates = new List<Employee>();    

    [Header("Name generator")]
    public List<string> firstNames = new List<string> { 
        "Elena", "Dan", "Diana", "Victor", "Ioana", "Andrei", "Maria", "Alexandru", "Cristina", "Mihai", "Ana", "Radu", "Sofia", "Gabriel" 
    };
    public List<string> lastNames = new List<string> { 
        "Popescu", "Ionescu", "Radu", "Stan", "Dumitrescu", "Gheorghe", "Matei", "Ciobanu", "Balan", "Marin", "Ilie", "Rusu", "Moldovan", "Nistor" 
    };

    void Awake()
    {
        Instance = this;
    }

    public void SendToUnassigned(Employee emp)
    {
        if (!unassignedEmployees.Contains(emp))
        {
            unassignedEmployees.Add(emp);
        }
    }

    private string GetRandomFullName()
    {
        string fName = firstNames[Random.Range(0, firstNames.Count)];
        string lName = lastNames[Random.Range(0, lastNames.Count)];
        return fName + "\n" + lName;
    }

    public void GenerateCandidates(int amount = 3)
    {
        marketCandidates.Clear();

        for (int i = 0; i < amount; i++)
        {
            string randomFullName = GetRandomFullName(); 
            marketCandidates.Add(new Employee(randomFullName));
        }
    }
}