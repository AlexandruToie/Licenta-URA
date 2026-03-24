using UnityEngine;

public enum SkillType { None, Management, Production, CustomerCare, Sales }

[System.Serializable]
public class Employee
{
    [Header("Identity")]
    public string employeeName;

    [Header("Status: Management")]
    public float managementSkill;
    public float managementPotential;

    [Header("Status: Production")]
    public float productionSkill;
    public float productionPotential;

    [Header("Status: Marketing")]
    public float marketingCustomerSkill;
    public float marketingCustomerPotential;
    public float marketingSalesSkill;
    public float marketingSalesPotential;

    [Header("Training System")]
    public bool isTraining = false;
    public float trainingTimer = 0f; 

    public SkillType currentlyTrainingSkill = SkillType.None;

    public Employee(string name)
    {
        employeeName = name;
        isTraining = false;

        managementSkill = Random.Range(5f, 20f);
        managementPotential = Random.Range(50f, 100f);

        productionSkill = Random.Range(5f, 20f);
        productionPotential = Random.Range(50f, 100f);

        marketingCustomerSkill = Random.Range(5f, 20f);
        marketingCustomerPotential = Random.Range(50f, 100f);

        marketingSalesSkill = Random.Range(5f, 20f);
        marketingSalesPotential = Random.Range(50f, 100f);
    }

    public float CalculateTrainingTime(SkillType skillToTrain)
    {
        float currentSkill = 0f;
        float potential = 0f;

        switch (skillToTrain)
        {
            case SkillType.Management:
                currentSkill = managementSkill;
                potential = managementPotential;
                break;
            case SkillType.Production:
                currentSkill = productionSkill;
                potential = productionPotential;
                break;
            case SkillType.CustomerCare:
                currentSkill = marketingCustomerSkill;
                potential = marketingCustomerPotential;
                break;
            case SkillType.Sales:
                currentSkill = marketingSalesSkill;
                potential = marketingSalesPotential;
                break;
        }

        float baseTime = 30f;
        float difficultyPenalty = Mathf.Pow(currentSkill / 10f, 1.5f) * 5f; 
        float talentBonus = (potential - currentSkill) * 0.5f;
        float finalTime = baseTime + difficultyPenalty - talentBonus;
        return Mathf.Max(15f, finalTime);
    }
}