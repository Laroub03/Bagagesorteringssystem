using System;
using System.Collections.Generic;
using System.Threading;
using Passengers;
using Baggages;

namespace MainClass
{
    public class BaggageSortingSystem
    {
        // Maximum buffer size for sorting
        private const int _maxBufferSize = 10;

        // Data structures for the different components: check-in queue, sorting buffer, and gates
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

        // CheckIn method handles passenger check-in process
        public void CheckIn()
        {
            int passengerCounter = 1;
            while (true)
            {
                // Produce 10 passengers in each iteration
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        lock(_checkInQueue)
                        {
                        // Create and enqueue a new passenger
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
                    }
                    catch (Exception e)
                    {
                        // handle any errors that occur
                        Console.WriteLine("Error caught.", e);
                    }
                }

                // Wait for sorting and gate loading processes to finish before generating more passengers
                while (true)
                {
                    bool checkInEmpty = false;
                    try
                    {
                        lock (_checkInQueue)
                        {
                            checkInEmpty = _checkInQueue.Count == 0;
                        }
                    }
                    catch (Exception e)
                    {
                        // handle any errors that occur
                        Console.WriteLine("Error caught.", e);
                    }

                    if (checkInEmpty)
                    {
                        break;
                    }

                    Thread.Sleep(1000);
                }
            }
        }

        // Sorting method handles the baggage sorting process
        public void Sorting()
        {
            while (true)
            {

                Baggage baggage = null;

                // Dequeue the next passenger's baggage from the check-in queue
                try
                {
                   lock(_checkInQueue)
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
                }
                catch (Exception e)
                {
                    // handle any errors that occur
                    Console.WriteLine("Error caught.", e);
                }


                if (baggage != null)
                {
                    try
                    {
                        lock (_sortingBuffer)
                        {
                            // Wait if the sorting buffer is full
                            while (_sortingBuffer.Count == _maxBufferSize)
                            {
                                Monitor.Wait(_sortingBuffer);
                            }

                            // Enqueue the baggage into the sorting buffer
                            _sortingBuffer.Enqueue(baggage);
                            Console.WriteLine($"Sorting: {baggage.BaggageNumber} for {baggage.FlightNumber}");

                            // Notify other threads if there is baggage in the sorting buffer
                            if (_sortingBuffer.Count > 0)
                            {
                                Monitor.PulseAll(_sortingBuffer);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // handle any errors that occur
                        Console.WriteLine("Error caught.", e);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        // GateLoading method handles the baggage loading process at the gates
        public void GateLoading()
        {
            while (true)
            {
                Baggage baggage = null;
                // Dequeue the next baggage from the sorting buffer
                try
                {
                    lock(_sortingBuffer)
                    {
                        if (_sortingBuffer.Count > 0)
                        {
                            baggage = _sortingBuffer.Dequeue();
                        }
                    }
                }
                catch (Exception e)
                {
                    // handle any errors that occur
                    Console.WriteLine("Error caught.", e);
                }

                if (baggage != null)
                {
                    // Determine the gate based on the flight number
                    string gateName = (baggage.FlightNumber == "Flight1") ? "Gate1" : "Gate2";

                    try
                    {
                        lock (_gates[gateName])
                        {
                        // Enqueue the baggage into the appropriate gate
                        _gates[gateName].Enqueue(baggage);
                        Console.WriteLine($"Loaded {baggage.BaggageNumber} to {gateName}");
                        Monitor.PulseAll(_gates[gateName]);
                        }
                    }
                    catch (Exception e)
                    {
                        // handle any errors that occur
                        Console.WriteLine("Error caught.", e);
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}