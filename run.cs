using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

class HotelCapacity
{
    private enum eventType
    {
        checkOut = -1,
        checkIn = 1
    }

    static bool CheckCapacity(int maxCapacity, List<Guest> guests)
    {
        var hotelEvents = new List<(DateTime date, eventType eventType)>(); //храним весь список событий
                                                                            //дату можно было оставить строкой, но на всякий случай DateTime

        foreach (var guest in guests) //полоняем список событий, помечая, какое из них заезд гостя, какое выезд
        {
            hotelEvents.Add((DateTime.Parse(guest.CheckOut), eventType.checkOut));
            hotelEvents.Add((DateTime.Parse(guest.CheckIn), eventType.checkIn));
        }

        hotelEvents.Sort(); //сортируем все события, сначала будут сортироваться по первому
                            //признаку(дате), затем по второму(тип операции). Выезды раньше, чем въезды!

        var currentGuests = 0; //тут будем хранить количество гостей в настоящий момент

        foreach (var hotelEvent in hotelEvents)
        {
            currentGuests += (int)hotelEvent.eventType; //если гость заехал, то количество
                                                        //гостей увеличится на 1, если
                                                        //уехал, то уменьшится
            if (currentGuests > maxCapacity) //если гостей больше, чем помещается, то не сможем уместить всех
                return false;
        }

        return true;
    }


    class Guest
    {
        public string Name { get; set; }
        public string CheckIn { get; set; }
        public string CheckOut { get; set; }
    }


    static void Main()
    {
        int maxCapacity = int.Parse(Console.ReadLine());
        int n = int.Parse(Console.ReadLine());


        List<Guest> guests = new List<Guest>();


        for (int i = 0; i < n; i++)
        {
            string line = Console.ReadLine();
            Guest guest = ParseGuest(line);
            guests.Add(guest);
        }

        bool result = CheckCapacity(maxCapacity, guests);

        Console.WriteLine(result ? "True" : "False");
    }

    static Guest ParseGuest(string json)
    {
        var guest = new Guest();

        Match nameMatch = Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
        if (nameMatch.Success)
            guest.Name = nameMatch.Groups[1].Value;

        Match checkInMatch = Regex.Match(json, "\"check-in\"\\s*:\\s*\"([^\"]+)\"");
        if (checkInMatch.Success)
            guest.CheckIn = checkInMatch.Groups[1].Value;

        Match checkOutMatch = Regex.Match(json, "\"check-out\"\\s*:\\s*\"([^\"]+)\"");
        if (checkOutMatch.Success)
            guest.CheckOut = checkOutMatch.Groups[1].Value;


        return guest;
    }
}