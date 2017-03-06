<!DOCTYPE HTML>

<html xmlns="http://www.w3.org/1999/xhtml" lang="en-CA">
	<head>
		<meta charset="utf-8" />
		<meta name="author" content="Tom Weatherhead" />
		<meta name="description" content="The labyrinthine library generator" />

		<title>Labyrinth Generator</title>

		<link rel="icon" type="image/png" href="favicon3.png" />

		<link rel="stylesheet" type="text/css" href="Style.css" />

		<!--
		<script type="text/javascript" src="jquery.js"></script>
		<script type="text/javascript" src="PageFooter.js"></script>
		<script type="text/javascript" src="DefaultDocumentReady.js"></script>
		-->
	</head>
	<body class="standardBody">
		<div id="topContent">Labyrinth Generator</div>

		<div id="mainContent" class="box centreText">
			<?php
				// PHP Labyrinth Generator - January 8, 2014

				// **** Class RoomInfo ****

				class RoomInfo {
					public $levelNumber;
					public $roomNumber;

					public function __construct($l, $r) {
						$this->levelNumber = $l;
						$this->roomNumber = $r;
					}

					/*
					public function Equals($otherRoom) {
						return $levelNumber == $otherRoom->levelNumber && $roomNumber == $otherRoom->roomNumber;
					}
					 */

					public function ToString() {
						$l = $this->levelNumber;
						$r = $this->roomNumber;
						return "($l, $r)";
					}

					private function GeneratePossibleNeighboursOnLevel($generator, $newLevel)
					{
					    $result = [];

					    if ($this->roomNumber == $generator->numberOfRoomsPerLevel - 1)
					    {
							// Rooms with this room number form the central core of the tower.

							for ($i = 0; $i < $generator->numberOfRoomsPerLevel - 1; ++$i)
							{
								$result[] = new RoomInfo($newLevel, $i);
							}
					    }
					    else
					    {
							$result[] = new RoomInfo($newLevel, ($this->roomNumber + 1) % ($generator->numberOfRoomsPerLevel - 1));
							$result[] = new RoomInfo($newLevel, ($this->roomNumber + $generator->numberOfRoomsPerLevel - 2) % ($generator->numberOfRoomsPerLevel - 1));
							$result[] = new RoomInfo($newLevel, $generator->numberOfRoomsPerLevel - 1);
					    }

					    return $result;
					}

					public function GeneratePossibleNeighbours($generator)
					{
					    $result = [];

					    if ($this->levelNumber > 0)
					    {
							$result = array_merge($result, $this->GeneratePossibleNeighboursOnLevel($generator, $this->levelNumber - 1));
					    }

					    if ($this->levelNumber < $generator->numberOfLevels - 1)
					    {
							$result = array_merge($result, $this->GeneratePossibleNeighboursOnLevel($generator, $this->levelNumber + 1));
					    }

					    return $result;
					}
				}

				// **** Class LabyrinthGenerator ****

				class LabyrinthGenerator {
					public $numberOfLevels;
					public $numberOfRoomsPerLevel;
					//public $numberOfExtraConnections;
					//public $numberOfExtraConnectionsAdded;
					//private $extraConnections;
					public $rooms;
					public $roomLabels;
					public $connections;
					public $openList;
					private $numberOfDifferentLabels;
					private $roomGoal;
					public $booksInRooms;
					private $numberOfAttemptsToRefactor;
					private $maximumNumberOfAttemptsToRefactor;

					public function __construct($l, $r /* , $xc */) {
						/*
						if ($l < 2 || $r < 4)
						{
						    throw new ArgumentException("Invalid parameter(s).");
						}
						 */

						$this->numberOfLevels = $l;
						$this->numberOfRoomsPerLevel = $r;
						//$numberOfExtraConnections;
						//$numberOfExtraConnectionsAdded = 0;
						//$extraConnections;
						$this->rooms = [];
						$this->roomLabels = [];
						$this->connections = [];
						$this->openList = [];
						$this->numberOfDifferentLabels = 0;
						$this->roomGoal = null;
						$this->booksInRooms = [];
						$this->numberOfAttemptsToRefactor = 0;
						$this->maximumNumberOfAttemptsToRefactor = 100;
					}

					private function FindConflictingConnections($room1, $room2)
					{
						// Test 0: Room labels ("blob numbers").

						/*
						if (roomLabels[room1] == roomLabels[room2])
						{
						    return true;    // There is a conflict.
						}
						 */

						// Test 1: Room 3 must not be connected to room 4.

						// 4  2
						//  \/
						//  /\
						// 1  3

						$room3 = new RoomInfo($room2->levelNumber, $room1->roomNumber);
						$room4 = new RoomInfo($room1->levelNumber, $room2->roomNumber);

						if (in_array($room4, $this->connections[$room3->ToString()]))
						{
						    return true;
						}

						// Test 2: Room 3 must not be connected to room 1.

						// 3
						//  \
						//   1
						//  /
						// 2

						$room3 = new RoomInfo(2 * $room1->levelNumber - $room2->levelNumber, $room2->roomNumber);

						//if (connections.ContainsKey(room3) && connections[room3].Contains(room1))
						if (in_array($room3, $this->connections[$room1->ToString()]))
						{
						    return true;
						}

						// Test 3: Room 3 must not be connected to room 2.

						// 3
						//  \
						//   2
						//  /
						// 1

						$room3 = new RoomInfo(2 * $room2->levelNumber - $room1->levelNumber, $room1->roomNumber);

						//if (connections.ContainsKey(room3) && connections[room3].Contains(room2))
						if (in_array($room3, $this->connections[$room2->ToString()]))
						{
						    return true;
						}

						return false;   // There is no conflict.
					}

					private function FindUnusedLabel()
					{
						$result = 0;
						$labels = array_values($this->roomLabels);

						while (in_array($result, $labels))
						{
						    ++$result;
						}

						return $result;
					}

					private function PropagateNewLabel($room, $newLabel, $addRoomsToOpenList)
					{
						$openListLocal = []; //new Stack<RoomInfo>();
						$closedList = []; //new HashSet<RoomInfo>();

						array_push($openListLocal, $room);

						while (count($openListLocal) > 0)
						{
						    $room = array_pop($openListLocal);
						    $this->roomLabels[$room->ToString()] = $newLabel;
						    $closedList[] = $room;

						    if ($addRoomsToOpenList)
						    {
						        $this->openList[] = $room;
						    }

						    foreach ($this->connections[$room->ToString()] as $room2)
						    {

						        if (!in_array($room2, $openListLocal) && !in_array($room2, $closedList))
						        {
									array_push($openListLocal, $room2);
						        }
						    }
						}
					}

					private function FindPossibleNeighboursWithDifferentLabels(&$room1, &$room2)
					{
						$openListLocal = $this->rooms; // Copy the array by value; see http://ca3.php.net/manual/en/language.types.array.php

						while (count($openListLocal) > 0)
						{
							$room1Index = array_rand($openListLocal);
						    //$room1 = openListLocal[random.Next(openListLocal.Count)];
						    //openListLocal.Remove(room1);
							$room1 = $openListLocal[$room1Index];
							array_splice($openListLocal, $room1Index, 1);

						    $possibleNeighbours = $room1->GeneratePossibleNeighbours($this);

						    while (count($possibleNeighbours) > 0)
						    {
								$room2Index = array_rand($possibleNeighbours);
						        //room2 = possibleNeighbours[random.Next(possibleNeighbours.Count)];
						        //possibleNeighbours.Remove(room2);
								$room2 = $possibleNeighbours[$room2Index];
								array_splice($possibleNeighbours, $room2Index, 1);

						        if ($this->roomLabels[$room1->ToString()] != $this->roomLabels[$room2->ToString()])
						        {
						            return;
						        }
						    }
						}

						throw new Exception("Unable to find possible neighbours with different labels.");
					}

					private function RemoveConnectionOneWay($room1, $room2) {
						$room1String = $room1->ToString();
						//$len = count($this->connections[$room1String]);

						//for ($i = 0; $i < $len; ++$i) { // Or: foreach array_keys(...)
						foreach (array_keys($this->connections[$room1String]) as $i) {

							if ($this->connections[$room1String][$i] == $room2) {
								unset($this->connections[$room1String][$i]);
								break;
							}
						}
					}

					private function RemoveConnectionBothWays($room1, $room2) {
						$this->RemoveConnectionOneWay($room1, $room2);
						$this->RemoveConnectionOneWay($room2, $room1);
					}

					private function Refactor()
					{
						echo "<p>Refactoring...</p>";

						$room1 = null;
						$room2 = null;

						$this->FindPossibleNeighboursWithDifferentLabels($room1, $room2);

						// Resolve the conflicts that are preventing a connection between room1 and room2.

						// Test 1: Room 3 must not be connected to room 4.

						// 4  2
						//  \/
						//  /\
						// 1  3

						$room3 = new RoomInfo($room2->levelNumber, $room1->roomNumber);
						$room4 = new RoomInfo($room1->levelNumber, $room2->roomNumber);

						if (in_array($room4, $this->connections[$room3->ToString()]))
						{
						    echo "<p>Found a Type 1 conflict.</p>";
							$this->RemoveConnectionBothWays($room3, $room4);
						    $this->PropagateNewLabel($room3, $this->FindUnusedLabel(), true);
						    $this->PropagateNewLabel($room4, $this->FindUnusedLabel(), true);
						}

						// Test 2: Room 3 must not be connected to room 1.

						// 3
						//  \
						//   1
						//  /
						// 2

						$room3 = new RoomInfo(2 * $room1->levelNumber - $room2->levelNumber, $room2->roomNumber);

						//if (connections.ContainsKey(room3) && connections[room3].Contains(room1))
						if (in_array($room3, $this->connections[$room1->ToString()]))
						{
						    echo "<p>Found a Type 2 conflict.</p>";
							$this->RemoveConnectionBothWays($room1, $room3);
						    $this->PropagateNewLabel($room3, $this->FindUnusedLabel(), true);
						}

						// Test 3: Room 3 must not be connected to room 2.

						// 3
						//  \
						//   2
						//  /
						// 1

						$room3 = new RoomInfo(2 * $room2->levelNumber - $room1->levelNumber, $room1->roomNumber);

						//if (connections.ContainsKey(room3) && connections[room3].Contains(room2))
						if (in_array($room3, $this->connections[$room2->ToString()]))
						{
						    echo "<p>Found a Type 3 conflict.</p>";
							$this->RemoveConnectionBothWays($room2, $room3);
						    $this->PropagateNewLabel($room3, $this->FindUnusedLabel(), true);
						}

						// Connect room1 and room2.
						$this->PropagateNewLabel($room2, $this->roomLabels[$room1->ToString()], false);
						$this->connections[$room1->ToString()][] = $room2;
						$this->connections[$room2->ToString()][] = $room1;

						$this->numberOfDifferentLabels = count(array_unique(array_values($this->roomLabels)));
					}

					private function FinalValidityCheck()
					{
						$this->PropagateNewLabel(new RoomInfo(0, 0), $this->FindUnusedLabel(), false);

						$this->numberOfDifferentLabels = count(array_unique(array_values($this->roomLabels)));

						if ($this->numberOfDifferentLabels > 1)
						{
							$n = $this->numberOfDifferentLabels;
						    throw new Exception("The labyrinth is in at least $n separate blobs.");
						}

						echo "<p>The labyrinth is a single blob.</p>";
					}

					//public function AddExtraConnections()

					public function Report()
					{
						echo "<table><tr><th>From</th><th>To</th></tr>";

						foreach ($this->rooms as $room)
						{
							$roomString = $room->ToString();

						    foreach ($this->connections[$roomString] as $otherRoom)
						    {
								//$otherRoomString = $otherRoom->ToString();
						        //echo "<tr><td>$roomString</td><td>$otherRoomString</td></tr>";
						        echo "<tr><td>$roomString</td><td>{$otherRoom->ToString()}</td></tr>"; // Complex string interpolation.
						    }
						}

						echo "</table>";

						/*
						if (numberOfExtraConnections > 0)
						{
			#if DEBUG_WRITELINE

						    foreach (var extraConnection in extraConnections)
						    {
						        Console.WriteLine("Extra connection added: {0} to {1}.", extraConnection.Key, extraConnection.Value);
						    }
			#endif

						    Console.WriteLine("{0} extra connection(s) requested; {1} added.", numberOfExtraConnections, numberOfExtraConnectionsAdded);
						}
						 */

						if ($this->numberOfAttemptsToRefactor > 0)
						{
							$n = $this->numberOfAttemptsToRefactor;
						    echo "<p>The labyrinth was refactored $n time(s).</p>";
						}

						$this->FinalValidityCheck();
					}

					private function FindShortestPathBetweenRooms($room, $roomGoalLocal)
					{
						$openListLocal = []; //new Queue<RoomInfo>();
						$paths = []; //new Dictionary<RoomInfo, List<RoomInfo>>();

						$openListLocal[] = $room;
						$paths[$room->ToString()] = [ $room ];

						if ($room == $roomGoalLocal)
						{
						    return $paths[$room->ToString()];
						}

						while (count($openListLocal) > 0)
						{
						    $room = array_shift($openListLocal);

						    foreach ($this->connections[$room->ToString()] as $room2)
						    {

						        if (!in_array($room2->ToString(), array_keys($paths)))    // paths.Keys is essentially the union of openListLocal and closedList.
						        {
						            $openListLocal[] = $room2;
						            $paths[$room2->ToString()] = $paths[$room->ToString()];
						            $paths[$room2->ToString()][] = $room2;

						            if ($room2 == $roomGoalLocal)
						            {
						                return $paths[$room2->ToString()];
						            }
						        }
						    }
						}

						// Here, room is the last room to be dequeued (and thus the last room to be enqueued).
						return $paths[$room->ToString()];
					}

					private function FindLongestPathFromRoom($room)
					{
						return $this->FindShortestPathBetweenRooms($room, null);
					}

					public function PrintLongestPath()
					{
						$path1 = $this->FindLongestPathFromRoom(new RoomInfo($this->numberOfLevels - 1, $this->numberOfRoomsPerLevel - 1));
						$longestPath = $this->FindLongestPathFromRoom($path1[count($path1) - 1]);

						//Console.WriteLine();
			//#if DEBUG_ONLY
						//Console.WriteLine("The longest path contains {0} rooms:", longestPath.Count);
						//Console.WriteLine(string.Join(" to ", longestPath));
			//#else
						$lengthOfLongestPath = count($longestPath);
						echo "<p>The longest path contains $lengthOfLongestPath rooms.</p>";
			//#endif

						$this->roomGoal = $longestPath[count($longestPath) - 1];

						$pathFromOriginToGoal = $this->FindShortestPathBetweenRooms(new RoomInfo(0, 0), $this->roomGoal);

						//Console.WriteLine();
			//#if DEBUG_ONLY
						//Console.WriteLine("Aristotle's Second Book of the Poetics is in Room {0}.", roomGoal);
						//Console.WriteLine();
						//Console.WriteLine("The path from Room (0, 0) to Room {0} contains {1} rooms:", roomGoal, pathFromOriginToGoal.Count);
						//Console.WriteLine(string.Join(" to ", pathFromOriginToGoal));
			//#else
						$lengthOfPathFromOriginToGoal = count($pathFromOriginToGoal);
						echo "<p>The path from Room (0, 0) to the goal contains $lengthOfPathFromOriginToGoal rooms.</p>";
			//#endif
					}

					private function PlaceBooksInRooms()
					{
						$books = [
						    "The First Book of the Poetics of Aristotle",
						    "The Iliad by Homer",
						    "The Odyssey by Homer",
						    "The Republic by Plato",
						    "Categories by Aristotle",
						    "Physics by Aristotle",
						    "Nicomachean Ethics by Aristotle",
						    "The Aeneid by Virgil",
						    "The Old Testament in Hebrew",
						    "The New Testament in Greek",
						    "Strong's Hebrew Dictionary",
						    "Strong's Greek Dictionary"
						];
						$openListLocal = $this->rooms;
						$numBooksPlaced = 1;

						$this->booksInRooms[$this->roomGoal->ToString()] = "The Second Book of the Poetics of Aristotle";
						//openListLocal.Remove(roomGoal);

						foreach (array_keys($openListLocal) as $key)
						{

							if ($openListLocal[$key] == $this->roomGoal)
							{
								unset($openListLocal[$key]);
							}
						}

						while ($numBooksPlaced * 3 < count($this->rooms) && count($books) > 0)
						{
							$roomIndex = array_rand($openListLocal);
							$bookIndex = array_rand($books);
							$room = $openListLocal[$roomIndex];
							$book = $books[$bookIndex];
						    //var room = openListLocal[random.Next(openListLocal.Count)];
						    //var book = books[random.Next(books.Count)];

							array_splice($openListLocal, $roomIndex, 1);
							array_splice($books, $bookIndex, 1);
						    //openListLocal.Remove(room);
						    //books.Remove(book);
						    $this->booksInRooms[$room->ToString()] = $book;
						    ++$numBooksPlaced;
						}
					}

					private function ReportBooks()
					{
						// Echo a table of the books and the rooms in which they are.
						echo "<table><tr><th>Room</th><th>Book</th></tr>";

						foreach (array_keys($this->booksInRooms) as $stringOfRoomContainingBook)
						{
							$bookInRoom = $this->booksInRooms[$stringOfRoomContainingBook];
							echo "<tr><td>$stringOfRoomContainingBook</td><td>$bookInRoom</td></tr>";
						}

						echo "</table>";
					}

					private function GetDatabaseConnection() {
						// MySQL with PDO_MYSQL
						// $DBH = new PDO("mysql:host=$host;dbname=$dbname", $user, $pass);
						$db = new PDO("mysql:host=localhost;dbname=labyrinth;charset=utf8", "user", "tomtom7");
						//echo "<p>Connected to the Labyrinth MySQL database.</p>";

						// Configure PHP/PDO to throw an exception if an error occurs.
						$db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

						return $db;
					}

					private function DeleteAllConnectionsInDatabase($db)
					{
						$stmt = $db->prepare("DELETE FROM connections");
						$stmt->execute();
					}

					private function DeleteAllBooksInDatabase($db)
					{
						$stmt = $db->prepare("DELETE FROM books");
						$stmt->execute();
					}

					private function InsertAllConnectionsIntoDatabase($db, $roomStringToRoom)
					{

						foreach (array_keys($this->connections) as $room1String)
						{
							$room1 = $roomStringToRoom[$room1String];

							foreach (array_values($this->connections[$room1String]) as $room2)
							{
								$stmt = $db->prepare("INSERT INTO connections (level1, room1, level2, room2) VALUES (:level1, :room1, :level2, :room2)");
			                    $stmt->bindValue(":level1", $room1->levelNumber, PDO::PARAM_INT);
			                    $stmt->bindValue(":room1", $room1->roomNumber, PDO::PARAM_INT);
			                    $stmt->bindValue(":level2", $room2->levelNumber, PDO::PARAM_INT);
			                    $stmt->bindValue(":room2", $room2->roomNumber, PDO::PARAM_INT);
								$stmt->execute();
							}
						}
					}

					private function InsertAllBooksIntoDatabase($db, $roomStringToRoom)
					{

						foreach (array_keys($this->booksInRooms) as $roomString)
						{
							$room = $roomStringToRoom[$roomString];

							$stmt = $db->prepare("INSERT INTO books (level, room, name) VALUES (:level, :room, :name)");
		                    $stmt->bindValue(":level", $room->levelNumber, PDO::PARAM_INT);
		                    $stmt->bindValue(":room", $room->roomNumber, PDO::PARAM_INT);
				            $stmt->bindValue(":name", $this->booksInRooms[$roomString], PDO::PARAM_STR);
							$stmt->execute();
						}
					}

					public function Generate()
					{
						$label = 0;

						$this->numberOfDifferentLabels = $this->numberOfLevels * $this->numberOfRoomsPerLevel;

						for ($l = 0; $l < $this->numberOfLevels; ++$l)
						{

						    for ($r = 0; $r < $this->numberOfRoomsPerLevel; ++$r)
						    {
						        $room = new RoomInfo($l, $r);

						        $this->rooms[] = $room;
						        $this->roomLabels[$room->ToString()] = $label++;
						        $this->connections[$room->ToString()] = [];
						        $this->openList[] = $room;
						    }
						}

						while ($this->numberOfDifferentLabels > 1)
						{

						    if (count($this->openList) == 0)
						    {

						        if ($this->numberOfAttemptsToRefactor >= $this->maximumNumberOfAttemptsToRefactor)
						        {
									$n = $this->numberOfAttemptsToRefactor;
						            throw new Exception("Attempted to refactor $n times; all failed.");
						        }

						        ++$this->numberOfAttemptsToRefactor;
						        $this->Refactor();
						    }

							$room1Index = array_rand($this->openList);
						    $room1 = $this->openList[$room1Index];
						    $possibleNeighbours = $room1->GeneratePossibleNeighbours($this);
						    $room2 = null;

						    while ($room2 == null && count($possibleNeighbours) > 0)
						    {
								$room2Index = array_rand($possibleNeighbours);
						        $room2 = $possibleNeighbours[$room2Index];

						        if ($this->roomLabels[$room1->ToString()] != $this->roomLabels[$room2->ToString()] &&
									!$this->FindConflictingConnections($room1, $room2))
						        {
						            break;
						        }

						        //possibleNeighbours.Remove(room2);
								unset($possibleNeighbours[$room2Index]);
						        $room2 = null;
						    }

						    if ($room2 == null)
						    {
						        //openList.Remove(room1);
								unset($this->openList[$room1Index]);
						        continue;
						    }

						    // We have now chosen room1 and room2.
						    $this->connections[$room1->ToString()][] = $room2;
						    $this->connections[$room2->ToString()][] = $room1;

						    // Join the two "blobs" to which the two rooms belong, by modifying room labels.
						    $label1 = $this->roomLabels[$room1->ToString()];
						    $label2 = $this->roomLabels[$room2->ToString()];
						    $minLabel = min($label1, $label2);
						    $maxLabel = max($label1, $label2);

						    foreach ($this->rooms as $room)
						    {

						        if ($this->roomLabels[$room->ToString()] == $maxLabel)
						        {
						            $this->roomLabels[$room->ToString()] = $minLabel;
						        }
						    }

						    --$this->numberOfDifferentLabels;
						}

						/*
						if (numberOfExtraConnections > 0)
						{
						    AddExtraConnections();
						}
						 */

						$this->Report();
						$this->PrintLongestPath();     // This sets roomGoal.
						$this->PlaceBooksInRooms();    // This uses roomGoal.
						$this->ReportBooks();

						// Now write the connections and the booksInRooms to the database.
						$roomStringToRoom = [];

						foreach (array_values($this->rooms) as $room)
						{
							$roomStringToRoom[$room->ToString()] = $room;
						}

						$db = $this->GetDatabaseConnection();

						$this->DeleteAllConnectionsInDatabase($db);
						$this->DeleteAllBooksInDatabase($db);
						$this->InsertAllConnectionsIntoDatabase($db, $roomStringToRoom);
						$this->InsertAllBooksIntoDatabase($db, $roomStringToRoom);

						// Close the connection.
						$db = null;
					}
				}

				// **** Non-class code ****

				try
				{
					$gen = new LabyrinthGenerator(15, 7);
					$gen->Generate();

					echo "<p>All done!</p>";
					echo '<p><a href="index.php">Navigate</a></p>';
				}
				catch (Exception $e)
				{
					echo "<p>Exception: " . $e->getMessage() . "</p>";
				}
			?>
		</div>
	</body>
</html>
