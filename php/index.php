<!DOCTYPE HTML>

<html xmlns="http://www.w3.org/1999/xhtml" lang="en-CA">
	<head>
		<meta charset="utf-8" />
		<meta name="author" content="Tom Weatherhead" />
		<meta name="description" content="The labyrinthine library" />

		<title>Labyrinth</title>

		<link rel="icon" type="image/png" href="favicon3.png" />

		<link rel="stylesheet" type="text/css" href="Style.css" />

		<!--
		<script type="text/javascript" src="jquery.js"></script>
		<script type="text/javascript" src="PageFooter.js"></script>
		<script type="text/javascript" src="DefaultDocumentReady.js"></script>
		-->
	</head>
	<body class="standardBody">
		<div id="topContent">Labyrinth</div>

		<div id="mainContent" class="box centreText">
			<?php
				// PHP Labyrinth - October 2, 2013

				// See http://net.tutsplus.com/tutorials/php/why-you-should-be-using-phps-pdo-for-database-access/

				// Additional MySQL database config (beyond what is needed by the .NET Labyrinth app):
				// CREATE TABLE visited (level int, room int, PRIMARY KEY (level, room));

				class RoomInfoWithPathToGoal {
					public $level;
					public $room;
					public $pathToGoal;

					function __construct() {
					}
				}

				function getDatabaseConnection() {
					// MySQL with PDO_MYSQL
					// $DBH = new PDO("mysql:host=$host;dbname=$dbname", $user, $pass);
					$db = new PDO("mysql:host=localhost;dbname=labyrinth;charset=utf8", "user", "tomtom7");
					//echo "<p>Connected to the Labyrinth MySQL database.</p>";

					// Configure PHP/PDO to throw an exception if an error occurs.
					$db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

					return $db;
				}

				function hasBeenVisited($db, $level, $room) {
					$stmt2 = $db->prepare("SELECT * FROM visited WHERE level = :level AND room = :room");
					$stmt2->bindValue(":level", $level, PDO::PARAM_INT);
					$stmt2->bindValue(":room", $room, PDO::PARAM_INT);
					$stmt2->execute();

					// We can only do a row count on a SELECT when we are using the PDO MySQL driver;
					// See http://wiki.hashphp.org/PDO_Tutorial_for_MySQL_Developers

					//return ($stmt->rowCount() > 0)	// This might work, but the following is more portable:
					return $stmt2->fetch(PDO::FETCH_ASSOC);	// Returns a value that can be interpreted as a Boolean.
				}

				function getBooksInRoom($db, $level, $room) {
					// Find the book(s) in this room, if any.
					$stmt = $db->prepare("SELECT name FROM books WHERE level = :level AND room = :room");
					$stmt->bindValue(":level", $level, PDO::PARAM_INT);
					$stmt->bindValue(":room", $room, PDO::PARAM_INT);
					$stmt->execute();
					$result = array();

					foreach ($stmt->fetchAll(PDO::FETCH_ASSOC) as $row)
					{
						$result[] = $row["name"];
					}

					return $result;
				}

				function isGoal($db, $level, $room) {

					foreach (getBooksInRoom($db, $level, $room) as $book) {
						// Look for "The Second Book of the Poetics of Aristotle".

						if (strpos($book, "Second") !== false) {
							return true;
						}
					}

					return false;
				}

				function getConnections($db, $level, $room) {
					// Find the connections from this room to other rooms.
					$stmt = $db->prepare("SELECT level2, room2 FROM connections WHERE level1 = :level1 AND room1 = :room1");
					$stmt->bindValue(":level1", $level, PDO::PARAM_INT);
					$stmt->bindValue(":room1", $room, PDO::PARAM_INT);
					$stmt->execute();
					$result = array();

					foreach ($stmt->fetchAll(PDO::FETCH_ASSOC) as $row) {
						$roomInfo = new RoomInfoWithPathToGoal();
						$roomInfo->level = $row["level2"];
						$roomInfo->room = $row["room2"];
						$roomInfo->pathToGoal = "No path generated.";
						$result[] = $roomInfo;
					}

					return $result;
				}

				function arrayContainsRoom($array, $roomInfo) {

					foreach ($array as $arrayElement) {

						if ($arrayElement->level == $roomInfo->level && $arrayElement->room == $roomInfo->room) {
							return true;
						}
					}

					return false;
				}

				function getPathToGoal($db, $level, $room) {
					$roomInfo = new RoomInfoWithPathToGoal();
					$roomInfo->level = $level;
					$roomInfo->room = $room;
					$roomInfo->pathToGoal = "(".$level.", ".$room.")";
					$queue = array($roomInfo);
					$closedSet = array();

					while (count($queue) > 0) {
						$roomInfo = array_shift($queue);
						$closedSet[] = $roomInfo;

						if (isGoal($db, $roomInfo->level, $roomInfo->room)) {
							return $roomInfo->pathToGoal;
						}

						foreach (getConnections($db, $roomInfo->level, $roomInfo->room) as $connectedRoom) {

							if (!arrayContainsRoom($queue, $connectedRoom) && !arrayContainsRoom($closedSet, $connectedRoom)) {
								$connectedRoom->pathToGoal = $roomInfo->pathToGoal." to (".$connectedRoom->level.", ".$connectedRoom->room.")";
								$queue[] = $connectedRoom;
							}
						}
					}

					return "No path to the goal was found.";
				}

				try {
					$db = getDatabaseConnection();

					$level = 0;
					$room = 0;

					if (isset($_GET["level"]) && isset($_GET["room"])) {
						$level = $_GET["level"];
						$room = $_GET["room"];
					} else {
						// Clear the "visited" table.
						$stmt = $db->prepare("DELETE FROM visited");
						$stmt->execute();
					}

					if (!hasBeenVisited($db, $level, $room)) {
						// Insert this room into the "visited" table.
						$stmt = $db->prepare("INSERT INTO visited (level, room) VALUES (:level, :room)");
						$stmt->bindValue(":level", $level, PDO::PARAM_INT);
						$stmt->bindValue(":room", $room, PDO::PARAM_INT);
						$stmt->execute();
					}

					echo "<p>You are in room (".$level.", ".$room.").</p>";

					// Find the book(s) in this room, if any.

					foreach (getBooksInRoom($db, $level, $room) as $book) {
						echo '<p>The book "'.$book.'" is in this room.</p>';
					}

					if (isGoal($db, $level, $room)) {
						echo "<p>**** Congratulations! You have reached the goal! ****</p>";
					}

					// Find the connections from this room to other rooms.
					echo "<table>";
					echo "<tr><th>Room</th><th>Visited</th></tr>";

					foreach (getConnections($db, $level, $room) as $connectedRoom) {
						$level2 = $connectedRoom->level;
						$room2 = $connectedRoom->room;

						$text = "(".$level2.", ".$room2.")";
						$link = "index.php?level=".$level2."&room=".$room2;
						$visited = "False";

						if (hasBeenVisited($db, $level2, $room2)) {
							$visited = "True";
						}

						echo "<tr>";
						echo "<td><a href='".$link."'>".$text."</a></td>";
						echo "<td>".$visited."</td>";
						echo "</tr>";
					}

					echo "</table>";

					if (isset($_GET["help"])) {
						echo "<p>Path to goal: ".getPathToGoal($db, $level, $room)."</p>";
					} else {
						echo "<p><a href='index.php?level=".$level."&room=".$room."&help=1'>Help</a></p>";
					}

					echo '<p><a href="gen.php">Generate a new labyrinth</a></p>';

					// Close the connection.
					$db = null;
				} catch(PDOException $e) {
					echo "<p>PDO exception: ".$e->getMessage()."</p>";
				}
			?>
		</div>
	</body>
</html>
