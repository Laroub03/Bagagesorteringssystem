using System;
using MainClass;
using Passengers;

namespace CheckIns
{
    public class CheckInCounter
    {
        private string _counterName;
        private BaggageSortingSystem _baggageSortingSystem;

        public CheckInCounter(string counterName, BaggageSortingSystem baggageSortingSystem)
        {
            _counterName = counterName;
            _baggageSortingSystem = baggageSortingSystem;
        }

        public void CheckIn(int passengerId)
        {
            lock (_baggageSortingSystem)
            {
                // Create and enqueue a new passenger
                Passenger passenger = new Passenger
                {
                    Name = $"Passenger{passengerId}",
                    FlightNumber = (passengerId % 2 == 0) ? "Flight1" : "Flight2",
                    BaggageNumber = $"Baggage{passengerId}"
                };

                _baggageSortingSystem.EnqueuePassenger(passenger);
                Console.WriteLine($"{_counterName} checked in: {passenger.Name}, {passenger.FlightNumber}, {passenger.BaggageNumber}");
            }
        }

    }
}
