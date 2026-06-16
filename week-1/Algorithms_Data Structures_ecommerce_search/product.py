class Product:
    def __init__(self,pid,name):
        self.pid=pid
        self.name=name
    def __repr__(self):
        return f'{self.pid} - {self.name}'
