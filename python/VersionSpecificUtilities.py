# VersionSpecificUtilities.py

import sys

python2 = sys.version_info.major == 2
python3 = sys.version_info.major == 3

# See https://stackoverflow.com/questions/1093322/how-do-i-check-what-version-of-python-is-running-my-script
# assert sys.version_info >= (2,5)
# print(sys.version_info)
# print(sys.version_info[0])
# print(sys.version_info.major)

# Note: Python does not have a switch...case statement.
# See e.g. https://www.pydanny.com/why-doesnt-python-have-switch-case.html

def test():

	if (sys.version_info.major == 3):
		print('VersionSpecificUtilities.test() : Python 3')
		# inputStr = input("Your move (or (h)elp or (q)uit): ") # Python 2's raw_input() is called "input()" in Python 3
	elif (sys.version_info.major == 2):
		print('VersionSpecificUtilities.test() : Python 2')
		# inputStr = raw_input("Your move (or (h)elp or (q)uit): ") # Python 2
	else:
		raise Exception('VersionSpecificUtilities.test() : Invalid version of Python: ' + str(sys.version_info))

def input(prompt):

	#if (sys.version_info.major == 3):
	if (python3):
		import builtins	# The module "builtins" is avaialble in Python 3, but not Python 2.
		return builtins.input(prompt) # Python 2's raw_input() is called "input()" in Python 3
	#elif (sys.version_info.major == 2):
	elif (python2):
		return raw_input(prompt) # Python 2
	else:
		raise Exception('VersionSpecificUtilities.input() : Invalid version of Python: ' + str(sys.version_info))
