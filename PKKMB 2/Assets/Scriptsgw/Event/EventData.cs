using System;
using System.Collections.Generic;

[Serializable]
public class EventQuest
{
    public string questId;
    public string question;
    public List<string> options;
    public string answer;
}

[Serializable]
public class EventQuiz
{
    public List<EventQuest> quests;
}

[Serializable]
public class EventDataQuiz
{
    public string id;
    public string title;
    public string description;
    public string location;
    public string startDate;
    public string endDate;
    public string image;
    public string createdAt;
    public EventQuiz quiz;
}

[Serializable]
public class EventListWrapper
{
    public List<EventDataQuiz> events;
}