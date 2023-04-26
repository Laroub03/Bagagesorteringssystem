using System;
using System.Collections.Generic;
using System.Threading;
using Passengers;
using Baggages;
using CheckIns;

namespace MainClass
{
    public class BaggageSortingSystem
    {
        private const int _maxBufferSize = 10;

        static Queue<Passenger> _checkInQueue = new Queue<Passenger>();
        static Queue<Baggage> _sortingBuffer = new Queue<Baggage>();
        static Dictionary<string, Queue<Baggage>> _gates = new Dictionary<string, Queue<Baggage>>();


        public static void Main(string[] args)
        {
            BaggageSortingSystem baggageSortingSystem = new BaggageSortingSystem();

            _gates.Add("Gate1", new Queue<Baggage>());
            _gates.Add("Gate2", new Queue<Baggage>());

            CheckInCounter checkInCounter1 = new CheckInCounter("Counter1", baggageSortingSystem);
            CheckInCounter checkInCounter2 = new CheckInCounter("Counter2", baggageSortingSystem);

            int passengerCounter = 1;

            while (true)
            {
                List<Thread> passengerThreads = new List<Thread>();

                for (int i = 0; i < 10; i++)
                {
                    int currentPassenger = passengerCounter;
                    Thread passengerThread = new Thread(() =>
                    {
                        Random rnd = new Random();
                        int counterIndex = rnd.Next(1, 3);
                        if (counterIndex == 1)
                        {
                            checkInCounter1.CheckIn(currentPassenger);
                        }
                        else
                        {
                            checkInCounter2.CheckIn(currentPassenger);
                        }
                    });

                    passengerThreads.Add(passengerThread);
                    passengerCounter++;
                }

                foreach (Thread passengerThread in passengerThreads)
                {
                    passengerThread.Start();
                    passengerThread.Join();
                }

                Thread sortingThread = new Thread(new ThreadStart(baggageSortingSystem.Sorting));
                Thread gateLoadingThread = new Thread(new ThreadStart(baggageSortingSystem.GateLoading));

                sortingThread.Start();
                gateLoadingThread.Start();

                sortingThread.Join();
                gateLoadingThread.Join();
            }
        }



        public void EnqueuePassenger(Passenger passenger)
        {
            lock (_checkInQueue)
            {
                _checkInQueue.Enqueue(passenger);
            }
        }

        public bool CheckInQueueEmpty()
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
                Console.WriteLine("Error caught.", e);
            }
            return checkInEmpty;
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

                if (CheckInQueueEmpty())
                {
                    break;
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

                if (_sortingBuffer.Count == 0 && CheckInQueueEmpty())
                {
                    break;
                }
                Thread.Sleep(1000);
            }
        }
    }
}