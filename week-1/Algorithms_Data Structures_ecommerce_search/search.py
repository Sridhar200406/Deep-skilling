def linear_search(products,target):
    for p in products:
        if p.name.lower()==target.lower(): return p
    return None

def binary_search(products,target):
    low,high=0,len(products)-1
    target=target.lower()
    while low<=high:
        mid=(low+high)//2
        n=products[mid].name.lower()
        if n==target:return products[mid]
        if n<target: low=mid+1
        else: high=mid-1
    return None
