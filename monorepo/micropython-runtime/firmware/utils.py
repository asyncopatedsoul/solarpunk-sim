import ast
import inspect
import importlib.util
import os

def remove_imports(file_path, imports_to_remove):
    """
    Removes specific import statements from a Python file.

    Args:
        file_path (str): The path to the Python file.
        imports_to_remove (list): A list of import statements to remove. 
                                 Each element should be a string representing 
                                 the full import line (e.g., 'from robot import Robot').
    """

    try:
        with open(file_path, "r") as f:
            tree = ast.parse(f.read())

        new_body = []
        for node in tree.body:
            if isinstance(node, (ast.Import, ast.ImportFrom)):
                import_statement = ast.unparse(node)  # Python 3.9+
                if import_statement not in imports_to_remove:
                    new_body.append(node)
            else:
                new_body.append(node)

        new_tree = ast.Module(body=new_body, type_ignores=[])
        new_code = ast.unparse(new_tree)

        new_file_path = file_path.replace(".py", "_new.py")
        with open(new_file_path, "w") as f:
            f.write(new_code)

        print(f"Successfully removed imports from '{file_path}'.")
        return new_file_path
        

    except FileNotFoundError:
        print(f"Error: File not found: {file_path}")
    except Exception as e:
        print(f"An error occurred: {e}")