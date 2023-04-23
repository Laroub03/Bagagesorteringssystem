using System;
using System.Collections.Generic;
using System.Threading;
using Passengers;
using Baggages;

namespace MainClass
{
    public class BaggageSortingSystem
    {
        // Maximum buffer size
        static int _maxBufferSize = 10;

        // Data structures for the different components
        static Queue<Passenger> _checkInQueue = new Queue<Passenger>();
        static Queue<Baggage> _sortingBuffer = new Queue<Baggage>();
        static Dictionary<string, Queue<Baggage>> _gates = new Dictionary<string, Queue<Baggage>>();

        public static void Main(string[] args)
        {
            BaggageSortingSystem baggageSortingSystem = new BaggageSortingSystem();

            // Initialize gates
            _gates.Add("Gate1", new Queue<Baggage>());
            _gates.Add("Gate2", new Queue<Baggage>());

            // Create and start the threads
            Thread checkInThread = new Thread(new ThreadStart(baggageSortingSystem.CheckIn));
            Thread sortingThread = new Thread(new ThreadStart(baggageSortingSystem.Sorting));
            Thread gateLoadingThread = new Thread(new ThreadStart(baggageSortingSystem.GateLoading));

            checkInThread.Start();
            sortingThread.Start();
            gateLoadingThread.Start();

            checkInThread.Join();
            sortingThread.Join();
            gateLoadingThread.Join();
        }

        // Implement the CheckIn, Sorting, and GateLoading methods
        public void CheckIn()
        {
            int passengerCounter = 1;

            while (true)
            {
                // Produce 10 passengers
                for (int i = 0; i < 10; i++)
                {
                    Monitor.Enter(_checkInQueue);
                    try
                    {
                        Passenger passenger = new Passenger
                        {
                            Name = $"Passenger{passengerCounter}",
                            FlightNumber = (passengerCounter % 2 == 0) ? "Flight1" : "Flight2",
                            BaggageNumber = $"Baggage{passengerCounter}"
                        };
                        _checkInQueue.Enqueue(passenger);
                        Console.WriteLine($"Checked in: {passenger.Name}, {passenger.FlightNumber}, {passenger.BaggageNumber}");
                        passengerCounter++;
                    }
                    finally
                    {
                        Monitor.Exit(_checkInQueue);
                    }
                }

                // Wait for sorting and gate loading to finish
                while (true)
                {
                    Monitor.Enter(_checkInQueue);
                    bool checkInEmpty = false;
                    try
                    {
                        checkInEmpty = _checkInQueue.Count == 0;
                    }
                    finally
                    {
                        Monitor.Exit(_checkInQueue);
                    }

                    if (checkInEmpty)
                    {
                        break;
                    }

                    Thread.Sleep(1000); // Wait for a second before checking again
                }
            }
        }


        public void Sorting()
        {
            while (true)
            {
                Monitor.Enter(_checkInQueue);
                Baggage baggage = null;

                try
                {
                    if (_checkInQueue.Count > 0)
                    {
                        Passenger passenger = _checkInQueue.Dequeue();
                        baggage = new Baggage
                        {
                            BaggageNumber = passenger.BaggageNumber,
                            FlightNumber = passenger.FlightNumber
                        };
                    }
                }
                finally
                {
                    Monitor.Exit(_checkInQueue);
                }

                if (baggage != null)
                {
                    Monitor.Enter(_sortingBuffer);

                    try
                    {
                        while (_sortingBuffer.Count == _maxBufferSize)
                        {
                            Monitor.Wait(_sortingBuffer);
                        }

                        _sortingBuffer.Enqueue(baggage);
                        Console.WriteLine($"Sorting: {baggage.BaggageNumber} for {baggage.FlightNumber}");

                        if (_sortingBuffer.Count > 0)
                        {
                            Monitor.PulseAll(_sortingBuffer);
                        }
                    }
                    finally
                    {
                        Monitor.Exit(_sortingBuffer);
                    }
                }

                //Random rnd = new Random();
                //int number = rnd.Next(1, 2000);
                Thread.Sleep(1000);
            }
        }

        public void GateLoading()
        {
            while (true)
            {
                Monitor.Enter(_sortingBuffer);
                Baggage baggage = null;

                try
                {
                    if (_sortingBuffer.Count > 0)
                    {
                        baggage = _sortingBuffer.Dequeue();
                    }
                }
                finally
                {
                    Monitor.Exit(_sortingBuffer);
                }

                if (baggage != null)
                {
                    string gateName = (baggage.FlightNumber == "Flight1") ? "Gate1" : "Gate2";
                    Monitor.Enter(_gates[gateName]);

                    try
                    {
                        _gates[gateName].Enqueue(baggage);
                        Console.WriteLine($"Loaded {baggage.BaggageNumber} to {gateName}");
                        Monitor.PulseAll(_gates[gateName]);
                    }
                    finally
                    {
                        Monitor.Exit(_gates[gateName]);
                    }
                }

                //Random rnd = new Random();
                //int number = rnd.Next(1, 2000);
                Thread.Sleep(1000);
            }
        }
    }
}