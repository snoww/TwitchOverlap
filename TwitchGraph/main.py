import community as community_louvain
import networkx as nx
import json
from colorhash import ColorHash
import datetime


dt = datetime.datetime.today()

date_dir = dt.strftime("%m-%Y")[1:]

G = nx.read_weighted_edgelist(f"data/{date_dir}/edges.csv", delimiter=",", nodetype=str)

partition = community_louvain.best_partition(G, resolution=0.25)


def normalize(s_range_max, s_range_min, t_range_max, t_range_min, num):
    return (num - s_range_min) / (s_range_max - s_range_min) * (t_range_max - t_range_min) + t_range_min


nodes = []

with open(f"data/{date_dir}/nodes.csv", "r", encoding="utf-8") as nodes_file:
    lines = nodes_file.readlines()
    max_size = int(lines[0].split(",")[-1])
    min_size = int(lines[-1].split(",")[-1])
    max_t = 200
    min_t = 10
    for line in lines:
        values = line.split(",")
        if values[0] in partition:
            nodes.append({
                "id": values[0],
                "name": values[1],
                "color": ColorHash(str(partition[values[0]])).hex,
                "size": normalize(max_size, min_size, max_t, min_t, int(values[2])),
            })

edges = []
with open(f"data/{date_dir}/edges.csv", "r", encoding="utf-8") as edges_file:
    for line in edges_file:
        values = line.split(",")
        edges.append({
            "source": values[0],
            "target": values[1]
        })


with open(f"data/{date_dir}/{dt.month}_{dt.year}_graph.json", "w", encoding="utf-8") as json_file:
    json_file.write(json.dumps({
        "nodes": nodes,
        "edges": edges,
    }))
